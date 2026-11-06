> 最后更新：2026-10-19

    # 笔试 C++ 速查（STL 容器 + 高频模板 + 题型识别）

> 用途：秋招笔试前速刷。大部分游戏公司 C++ / Java / Python3 三选一，C++ 是默认项。
> 策略：不重刷全部算法，只过「STL 对照 + 高频模板 + 题型识别信号」，每题手敲一遍 C++ 版。

---

## 一、输入输出模板（牛客 / 赛码 C++ 格式）

```cpp
#include <bits/stdc++.h>          // 万能头，笔试专用（工程里别这么写）
using namespace std;

int main() {
    ios::sync_with_stdio(false);  // ★ 两行加速 IO，不加可能 TLE
    cin.tie(nullptr);

    // 读单个 int
    int n;
    cin >> n;

    // 读 n 个 int 到数组
    vector<int> a(n);
    for (int i = 0; i < n; i++) cin >> a[i];

    // 读一行字符串（不含空格）
    string s;
    cin >> s;

    // 读整行（含空格）—— ⚠️ 前面用过 cin >> 必须先吃掉换行
    cin.ignore();
    string line;
    getline(cin, line);

    // 读到 EOF（多组输入，最常见）
    int x;
    while (cin >> x) {
        // 处理 x
    }

    // 一行空格分隔、个数不定 → stringstream 拆
    string row;
    while (getline(cin, row)) {
        if (row.empty()) continue;
        stringstream ss(row);
        int v;
        vector<int> nums;
        while (ss >> v) nums.push_back(v);
    }

    // 输出：用 '\n' 不要用 endl（endl 每次强制 flush，输出量大时会慢）
    cout << ans << '\n';

    return 0;
}
```

⚠️ **格式注意**：有些题要求输出"用空格分隔的一行"，有些是"每行一个"，**输出格式错了等于全错**。不确定就先看样例。

---

## 二、C# → C++ 容器对照表

| C# | C++ | 常用 API |
|-----|-----|---------|
| `List<int>` | `vector<int>` | `push_back / size() / [] / pop_back` |
| `Dictionary<K,V>` | `unordered_map<K,V>` | `count / find / []` |
| `SortedDictionary<K,V>` | `map<K,V>` | 同上，遍历时有序 |
| `Queue<T>` | `queue<T>` | `push / pop / front / size` |
| `Stack<T>` | `stack<T>` | `push / pop / top / size` |
| `PriorityQueue<T,P>` | `priority_queue<T>` | ⚠️ **默认大顶堆**（见下方坑点） |
| `(int a, int b)` 元组 | `pair<int,int>` | `make_pair / .first / .second` |
| `string`（不可变） | `string`（可变） | `s[i]` 可读**可写**，`substr / find` |
| `Array.Sort(arr)` | `sort(v.begin(), v.end())` | 升序默认 |
| `Array.Sort(arr, (a,b) => ...)` | `sort(v.begin(), v.end(), cmp)` | cmp 也可以是 lambda |
| `Array.BinarySearch(arr, x)` | `lower_bound / upper_bound` | ⚠️ 找不到返回 `end()`，**不是负数** |
| `HashSet<T>` | `unordered_set<T>` | `insert / count / erase` |
| `Array.Fill(arr, 0)` | `vector<int> v(n, 0)` | 直接构造时给初值 |
| `arr.Length` | `v.size()` | 数组才用 `sizeof(a)/sizeof(a[0])` |
| `int[,] dp = new int[n,m]` | `vector<vector<int>> dp(n, vector<int>(m));` | `dp[i][j]` 访问 |
| `long` | `long long` | 笔试数据大时优先 |

**常用一行流**（笔试省时间）：

```cpp
sort(v.begin(), v.end());                              // 升序
sort(v.begin(), v.end(), greater<int>());              // 降序
accumulate(v.begin(), v.end(), 0LL);                   // 求和（0LL 防溢出）
*max_element(v.begin(), v.end());                      // 最大值
v.erase(unique(v.begin(), v.end()), v.end());          // 去重（需先 sort）
reverse(v.begin(), v.end());                           // 反转
```

---

## 三、C++ 独有的坑（C# 里不会踩，这里会）

这几条是**换语言后最容易送分的地方**，笔试前一定扫一遍：

1. **`priority_queue` 默认是大顶堆**。C# 的 `PriorityQueue` 默认是小顶堆，直接照搬会静默出错。
   ```cpp
   priority_queue<int> maxHeap;                              // 大顶堆
   priority_queue<int, vector<int>, greater<int>> minHeap;   // 小顶堆
   // 自定义类型：priority_queue<Node, vector<Node>, cmp>，cmp 返回 true 表示 a 的优先级低于 b
   ```

2. **`map[key]` / `unordered_map[key]` 会插入默认值**。只想查别用 `[]`，用 `count()` 或 `find()`，否则会污染容器（遍历时多出一堆 0）。

3. **`sort` 的比较函数必须是严格弱序**。写 `return a <= b;` 在数据有重复时会越界 RE —— 必须写 `return a < b;`。

4. **数组越界不报错**。C# 越界抛异常，C++ 是直接读脏内存，可能 AC 也可能莫名 WA/RE。**边界条件自己卡死**。

5. **`int` 溢出静默发生**。`int a = 1e9; a * 2` 不报错，直接变负数。乘法前先转：`(long long)a * b`。

6. **`vector<int> v(n)` vs `vector<int> v[n]`**。前者是 n 个 0，后者是 n 个空 vector（VLA，非标准）。别写混。

7. **`size()` 返回无符号数**。`for (int i = 0; i < v.size() - 1; i++)` 在空 vector 时会变成巨大正数 → 死循环。习惯写 `(int)v.size()`。

---

## 四、高频题型最小模板（每题 10 分钟手敲一遍）

### 1. 二分查找（手写最稳，`lower_bound` 语义容易记反）
```cpp
// 找第一个 >= target 的位置（左边界）
int LowerBound(const vector<int>& a, int target) {
    int l = 0, r = a.size();          // 左闭右开
    while (l < r) {
        int mid = l + (r - l) / 2;    // 防溢出写法
        if (a[mid] < target) l = mid + 1;
        else r = mid;
    }
    return l;                          // 返回插入位置
}
```

### 2. 滑动窗口（找最长满足条件的子数组）
```cpp
int SlidingWindow(const vector<int>& a, int k) {
    int l = 0, ans = 0, sum = 0;
    for (int r = 0; r < (int)a.size(); r++) {
        sum += a[r];                       // 扩右边界
        while (sum > k) {                  // 不满足 → 缩左边界
            sum -= a[l];
            l++;
        }
        ans = max(ans, r - l + 1);         // 更新答案
    }
    return ans;
}
```

### 3. 哈希（两数之和）
```cpp
vector<int> TwoSum(const vector<int>& nums, int target) {
    unordered_map<int, int> mp;
    for (int i = 0; i < (int)nums.size(); i++) {
        auto it = mp.find(target - nums[i]);
        if (it != mp.end()) return { it->second, i };
        mp[nums[i]] = i;
    }
    return { -1, -1 };
}
```

### 4. DFS（二叉树，含递归模板）
```cpp
struct TreeNode {
    int val;
    TreeNode *left = nullptr, *right = nullptr;
};

int Dfs(TreeNode* node) {
    if (!node) return 0;
    int left = Dfs(node->left);
    int right = Dfs(node->right);
    return max(left, right) + 1;   // 例：最大深度
}
```

### 5. BFS（图 / 矩阵最短步数）
```cpp
int Bfs(const vector<vector<int>>& grid, int startR, int startC) {
    int n = grid.size(), m = grid[0].size();
    queue<pair<int,int>> q;
    vector<vector<bool>> visited(n, vector<bool>(m, false));
    q.push({startR, startC});
    visited[startR][startC] = true;

    int dr[4] = { -1, 1, 0, 0 };
    int dc[4] = { 0, 0, -1, 1 };
    int steps = 0;

    while (!q.empty()) {
        int size = q.size();              // 按层处理，方便计步数
        for (int i = 0; i < size; i++) {
            int r = q.front().first;
            int c = q.front().second;
            q.pop();
            // 到终点 → return steps

            for (int d = 0; d < 4; d++) {
                int nr = r + dr[d], nc = c + dc[d];
                if (nr < 0 || nr >= n || nc < 0 || nc >= m) continue;
                if (visited[nr][nc]) continue;
                visited[nr][nc] = true;
                q.push({nr, nc});
            }
        }
        steps++;
    }
    return -1;
}
```
> C++17 起可以写 `auto [r, c] = q.front();`，但有些老评测机只到 C++14，**笔试求稳就用 `.first / .second`**。

### 6. DP（0-1 背包）
```cpp
// 容量 cap，物品重量 w[i] 价值 v[i]
int Knapsack(int cap, const vector<int>& w, const vector<int>& v) {
    vector<int> dp(cap + 1, 0);
    for (int i = 0; i < (int)w.size(); i++)
        for (int j = cap; j >= w[i]; j--)        // 逆序：0-1 背包
            dp[j] = max(dp[j], dp[j - w[i]] + v[i]);
    return dp[cap];
}
```
> 完全背包把内层改成**正序** `for (int j = w[i]; j <= cap; j++)`，别的不用动。

### 7. 快排（万一要求手写排序）
```cpp
void QuickSort(vector<int>& a, int l, int r) {
    if (l >= r) return;
    int i = l, j = r, pivot = a[l + (r - l) / 2];
    while (i <= j) {
        while (a[i] < pivot) i++;
        while (a[j] > pivot) j--;
        if (i <= j) { swap(a[i], a[j]); i++; j--; }
    }
    QuickSort(a, l, j);
    QuickSort(a, i, r);
}
```

---

## 五、题型识别信号（比背模板更重要）

刷题"做过但遇到新题不会"，通常是缺这一层：**从题目特征直接映射到解法**，而不是去回忆某道原题。

| 题目里出现这些特征 | 大概率是 |
|---|---|
| 数组**有序** + 找某个位置 / 最小值最大化 | 二分 |
| **连续子数组 / 子串** + 最长最短满足某条件 | 滑动窗口 / 双指针 |
| 要求 **O(n)** 且需要"之前出现过吗" | 哈希表 |
| **最短路 / 最少步数 / 逐层扩散** | BFS |
| **所有方案 / 全排列 / 子集 / 棋盘** | 回溯（DFS + 撤销） |
| **网格连通块 / 岛屿** | DFS 或并查集 |
| **前 K 个 / 第 K 大 / 动态取最值** | 堆（`priority_queue`） |
| **子数组求和 / 区间查询** | 前缀和 / 差分 |
| **能不能凑出 / 最大价值 / 方案数** | DP |
| **下一个更大 / 左右第一个比它小** | 单调栈 |
| 数据范围 **n ≤ 20** | 状压 / 暴力枚举 |
| 数据范围 **n ≤ 10⁵** | 只能 O(n) 或 O(n log n) |
| 数据范围 **n ≤ 10³** | 才敢 O(n²) |

> 做题时的正确顺序：**先看数据范围定复杂度 → 再看题目特征定题型 → 最后才写代码。**
> 直接想"这题我做过吗"，就是在赌原题。

---

## 六、笔试前 10 分钟 checklist

- [ ] `#include <bits/stdc++.h>` + `int main()`，别写成 `class Program`
- [ ] `ios::sync_with_stdio(false); cin.tie(nullptr);` 加了吗
- [ ] **数据范围决定复杂度**：n ≤ 10⁵ 只能 O(n)/O(nlogn)；n ≤ 10³ 才敢 O(n²)
- [ ] 溢出：求和 / 乘法先想 `long long`
- [ ] 边界：空数组、单元素、全是同一个数、n=1
- [ ] `v.size()` 前面加 `(int)`，避免无符号数比较
- [ ] 输出格式：空格 vs 换行、末尾不能多空格（部分平台严格）
- [ ] 输出用 `'\n'` 不用 `endl`
- [ ] 不会的题先暴力拿部分分（笔试**按用例给分**），别死磕一题

---

## 七、笔试前 2 小时安排

| 时间 | 内容 |
|------|------|
| 30 分钟 | 过第二、三节：容器对照 + C++ 独有的 7 个坑（大顶堆、`map[]` 插入、严格弱序最要命） |
| 60 分钟 | 手敲第四节模板 1-7 各一遍，**在牛客 C++ 模式下跑通 I/O**，别只在脑子里过 |
| 30 分钟 | 随机抽 2 道之前刷过的题，用 C++ 重做（感受和 C# 的差异，别贪多） |

> 心态：笔试是"过了就行"，不是"满分才行"。语法不熟没关系，**模板会背 + I/O 不挂 + 暴力分拿满**，过笔试够了。
