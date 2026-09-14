#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
纯标准库 PDF 文本提取器 v2（支持中文 CID 字体 + ToUnicode CMap + 字体资源解析）
用法: python pdf_extract.py <input.pdf> <output.txt>
"""
import sys, re, zlib

def get_objects(data):
    objs = {}
    for m in re.finditer(rb'(\d+)\s+\d+\s+obj\b(.*?)endobj', data, re.S):
        objs[int(m.group(1))] = m.group(2)
    return objs

def get_stream(block):
    m = re.search(rb'stream\r?\n(.*?)endstream', block, re.S)
    if not m:
        return None
    raw = m.group(1)
    if raw.endswith(b'\r\n'):
        raw = raw[:-2]
    elif raw.endswith(b'\n'):
        raw = raw[:-1]
    try:
        return zlib.decompress(raw)
    except Exception:
        return raw

def parse_cmap(stream):
    mapping = {}
    if not stream:
        return mapping
    s = stream.decode('latin-1', errors='ignore')
    for m in re.finditer(r'beginbfchar(.*?)endbfchar', s, re.S):
        for pair in re.finditer(r'<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>', m.group(1)):
            src = int(pair.group(1), 16)
            dst_hex = pair.group(2)
            dst = [int(dst_hex[i:i+4], 16) for i in range(0, len(dst_hex), 4)]
            mapping[src] = dst
    for m in re.finditer(r'beginbfrange(.*?)endbfrange', s, re.S):
        for rng in re.finditer(r'<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>', m.group(1)):
            lo, hi = int(rng.group(1), 16), int(rng.group(2), 16)
            base = int(rng.group(3), 16)
            # 上限保护：超大范围（如 Identity 全映射）只生成实际长度
            if hi - lo > 100000:
                continue
            for i in range(lo, hi + 1):
                mapping[i] = [base + (i - lo)]
    return mapping

def clean_surrogates(s):
    """过滤孤立代理对字符（CMap 脏数据），避免写入 UTF-8 失败"""
    return ''.join(c for c in s if not (0xD800 <= ord(c) <= 0xDFFF))

def decode_hex_str(hexstr, cmap):
    h = hexstr.strip()
    if not h:
        return ''
    out = []
    # Identity-H / CID 字体：每字符 2 字节（4 位 hex）
    if len(h) % 4 == 0:
        chunks = [h[i:i+4] for i in range(0, len(h), 4)]
        for c in chunks:
            v = int(c, 16)
            if v in cmap:
                out.append(clean_surrogates(''.join(chr(x) for x in cmap[v])))
            else:
                out.append('?')
        return ''.join(out)
    for i in range(0, len(h), 2):
        v = int(h[i:i+2], 16)
        if v in cmap:
            out.append(clean_surrogates(''.join(chr(x) for x in cmap[v])))
        else:
            out.append('?')
    return ''.join(out)

def decode_literal(s):
    out = []
    i = 0
    while i < len(s):
        c = s[i]
        if c == '\\' and i + 1 < len(s):
            nxt = s[i+1]
            mp = {'n': '\n', 'r': '\r', 't': '\t', 'b': '\b', 'f': '\f',
                  '(': '(', ')': ')', '\\': '\\'}
            if nxt in mp:
                out.append(mp[nxt]); i += 2; continue
            if nxt.isdigit():
                j = i + 1
                num = ''
                while j < len(s) and s[j].isdigit() and len(num) < 3:
                    num += s[j]; j += 1
                out.append(chr(int(num, 8))); i = j; continue
            out.append(nxt); i += 2; continue
        out.append(c); i += 1
    return ''.join(out)

def find_dict(block, start):
    """从 start 起找 <</>> 平衡字典，返回字典内容 bytes（不含定界符）"""
    i = block.find(b'<<', start)
    if i < 0:
        return None
    depth = 0
    j = i
    while j < len(block):
        if block[j:j+2] == b'<<':
            depth += 1; j += 2
        elif block[j:j+2] == b'>>':
            depth -= 1; j += 2
            if depth == 0:
                return block[i+2:j-2]
        else:
            j += 1
    return None

def parse_font_resources(page_block, objs):
    """从页面对象解析 /Resources /Font 字典: {资源名: 字体对象号}
    用平衡括号解析，避免 /ExtGState 等嵌套字典提前截断。"""
    fonts = {}
    res = find_dict(page_block, page_block.find(b'/Resources'))
    if res is None:
        # Resources 可能是间接引用 /Resources n 0 R
        m = re.search(rb'/Resources\s+(\d+)\s+\d+\s+R', page_block)
        if m:
            res = objs.get(int(m.group(1)), b'')
    if not res:
        return fonts
    fm = find_dict(res, res.find(b'/Font'))
    if not fm:
        return fonts
    for pair in re.finditer(rb'/(\w+)\s+(\d+)\s+\d+\s+R', fm):
        fonts[pair.group(1).decode('ascii')] = int(pair.group(2))
    return fonts

def collect_cmaps(objs):
    """{字体对象号: cmap dict}"""
    cmaps = {}
    for num, block in objs.items():
        if b'/ToUnicode' in block:
            m = re.search(rb'/ToUnicode\s+(\d+)\s+\d+\s+R', block)
            if m:
                cmap_obj = int(m.group(1))
                stream = get_stream(objs.get(cmap_obj, b''))
                cmaps[num] = parse_cmap(stream)
    return cmaps

def page_contents(page_block, objs):
    """提取页面所有内容流（支持单个引用和数组引用）"""
    streams = []
    m_arr = re.search(rb'/Contents\s*\[(.*?)\]', page_block, re.S)
    if m_arr:
        for ref in re.finditer(rb'(\d+)\s+\d+\s+R', m_arr.group(1)):
            s = get_stream(objs.get(int(ref.group(1)), b''))
            if s:
                streams.append(s)
        return streams
    m = re.search(rb'/Contents\s+(\d+)\s+\d+\s+R', page_block)
    if m:
        s = get_stream(objs.get(int(m.group(1)), b''))
        if s:
            streams.append(s)
    return streams

def render_content(content, fonts, cmaps):
    """渲染内容流：跟踪当前字体（Tf），提取 Tj/TJ 文本"""
    parts = []
    cur_font = None
    # 匹配：Tf 设置字体 | <hex> Tj | (lit) Tj | [..] TJ
    pattern = re.compile(
        rb'/(\w+)\s+[\d.]+\s+Tf'
        rb'|<([0-9A-Fa-f]+)>\s*Tj'
        rb'|\(((?:[^()\\]|\\.)*)\)\s*Tj'
        rb'|\[(.*?)\]\s*TJ', re.S)
    for m in pattern.finditer(content):
        fname, hexstr, litstr, tjarr = m.groups()
        if fname:
            cur_font = fname.decode('ascii')
            continue
        cmap = {}
        if cur_font and cur_font in fonts:
            cmap = cmaps.get(fonts[cur_font], {})
        if hexstr:
            parts.append(decode_hex_str(hexstr.decode('ascii'), cmap))
        elif litstr:
            parts.append(decode_literal(litstr.decode('latin-1')))
        elif tjarr:
            for hx, lit in re.findall(rb'<([0-9A-Fa-f]+)>|\(((?:[^()\\]|\\.)*)\)', tjarr, re.S):
                if hx:
                    parts.append(decode_hex_str(hx.decode('ascii'), cmap))
                elif lit:
                    parts.append(decode_literal(lit.decode('latin-1')))
            parts.append(' ')
    return ''.join(parts)

def extract_text(data):
    objs = get_objects(data)
    cmaps = collect_cmaps(objs)
    pages_text = []
    for num, block in sorted(objs.items()):
        if not re.search(rb'/Type\s*/Page(?![s])', block):
            continue
        fonts = parse_font_resources(block, objs)
        streams = page_contents(block, objs)
        content = b'\n'.join(streams)
        if not content:
            continue
        pages_text.append(render_content(content, fonts, cmaps))
    return pages_text

def main():
    if len(sys.argv) < 3:
        print('usage: pdf_extract.py <in.pdf> <out.txt>')
        return 1
    with open(sys.argv[1], 'rb') as f:
        data = f.read()
    pages = extract_text(data)
    with open(sys.argv[2], 'w', encoding='utf-8') as f:
        for i, p in enumerate(pages, 1):
            f.write(f'\n===== Page {i} =====\n')
            f.write(p)
            f.write('\n')
    print(f'OK: {len(pages)} pages -> {sys.argv[2]}')
    return 0

if __name__ == '__main__':
    sys.exit(main())
