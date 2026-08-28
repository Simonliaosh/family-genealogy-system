# HSR业务系统 - wwwroot 静态资源目录

## 📁 目录结构

```
wwwroot/
├── css/                    # 样式文件
│   ├── site.css           # 主站样式（完整的UI组件）
│   └── login.css          # 登录页面样式
├── js/                     # JavaScript文件
│   └── site.js            # 主站功能（完整的前端功能）
├── images/                 # 图片资源
│   └── logo.png           # 系统Logo（需要您提供）
└── lib/                    # 第三方库（可选）
    └── README.md          # 第三方库说明
```

## ✅ 已提供的完整功能

### 📄 site.css（主站样式）

包含完整的UI组件库：

#### 布局组件
- ✅ 侧边栏导航（固定左侧，250px宽）
- ✅ 顶部导航栏（固定顶部，白色背景）
- ✅ 主内容区（响应式容器）
- ✅ 卡片组件（Card）

#### 表格组件
- ✅ 响应式表格
- ✅ 表格悬停效果
- ✅ 表格操作按钮区
- ✅ 表格斑马纹（可选）

#### 表单组件
- ✅ 输入框（text, select, textarea）
- ✅ 表单网格布局
- ✅ 复选框样式
- ✅ 表单验证样式

#### 按钮组件
- ✅ 主要按钮（primary）
- ✅ 成功按钮（success）
- ✅ 警告按钮（warning）
- ✅ 危险按钮（danger）
- ✅ 次要按钮（secondary）
- ✅ 按钮组（btn-group）
- ✅ 小按钮（btn-sm）

#### 消息提示
- ✅ 成功提示（alert-success）
- ✅ 错误提示（alert-danger）
- ✅ 警告提示（alert-warning）
- ✅ 信息提示（alert-info）

#### 其他组件
- ✅ 徽章（badge）
- ✅ 模态框（modal）
- ✅ 加载动画（loading & spinner）
- ✅ 分页组件
- ✅ 搜索栏

#### 响应式设计
- ✅ 移动端适配（768px断点）
- ✅ 平板适配
- ✅ 打印样式

### 📄 site.js（主站功能）

包含完整的前端功能库：

#### 核心功能
- ✅ 页面初始化
- ✅ DOM加载管理
- ✅ 全局配置

#### 用户交互
- ✅ 确认对话框（confirmDelete, confirmAction）
- ✅ 消息提示（showSuccess, showError, showWarning, showInfo）
- ✅ 加载动画（showLoading, hideLoading）
- ✅ 模态框控制（showModal, hideModal）

#### 表格功能
- ✅ 实时搜索（searchTable）
- ✅ 搜索结果统计
- ✅ 表格导出Excel（exportTableToExcel）

#### 表单功能
- ✅ 表单验证（validateForm）
- ✅ 必填字段检查
- ✅ 错误提示显示
- ✅ 表单重置（resetForm）

#### AJAX请求
- ✅ 统一请求封装（ajax）
- ✅ GET请求（ajaxGet）
- ✅ POST请求（ajaxPost）
- ✅ PUT请求（ajaxPut）
- ✅ DELETE请求（ajaxDelete）
- ✅ 错误处理

#### 工具函数
- ✅ 日期格式化（formatDate）
- ✅ 数字格式化（formatNumber）
- ✅ 货币格式化（formatCurrency）
- ✅ 防抖（debounce）
- ✅ 节流（throttle）
- ✅ 复制到剪贴板（copyToClipboard）

#### 权限管理
- ✅ 权限检查（hasPermission）
- ✅ 权限验证（checkPermission）

#### 其他功能
- ✅ 侧边栏控制（移动端）
- ✅ 活动菜单高亮
- ✅ 工具提示（tooltip）
- ✅ 打印功能（printPage, printElement）

### 📄 login.css（登录页面）

专业的登录页面样式：

- ✅ 渐变背景
- ✅ 居中卡片布局
- ✅ 输入框图标
- ✅ 悬停动画
- ✅ 记住我复选框
- ✅ 错误/成功消息样式
- ✅ 响应式设计

## 🚀 使用方法

### 1. 在Layout中引用

```html
<!DOCTYPE html>
<html>
<head>
    <title>HSR业务系统</title>
    <!-- 引用主站样式 -->
    <link rel="stylesheet" href="~/css/site.css" />
</head>
<body>
    <!-- 侧边栏 -->
    <div class="sidebar">
        <div class="sidebar-header">
            <a href="/" class="sidebar-brand">HSR系统</a>
        </div>
        <ul class="sidebar-menu">
            <li><a href="/"><i>🏠</i>首页</a></li>
            <li><a href="/Employee"><i>👥</i>员工管理</a></li>
            <li><a href="/Position"><i>💼</i>岗位管理</a></li>
        </ul>
    </div>
    
    <!-- 主内容 -->
    <div class="main-content">
        <div class="navbar">
            <div class="navbar-left">
                <h1>@ViewData["Title"]</h1>
            </div>
            <div class="navbar-right">
                <div class="user-info">
                    <div class="user-avatar">Admin</div>
                    <span>管理员</span>
                </div>
            </div>
        </div>
        
        <div class="container">
            @RenderBody()
        </div>
    </div>
    
    <!-- 引用JavaScript -->
    <script src="~/js/site.js"></script>
</body>
</html>
```

### 2. 在登录页面中使用

```html
<!DOCTYPE html>
<html>
<head>
    <title>登录 - HSR系统</title>
    <link rel="stylesheet" href="~/css/login.css" />
</head>
<body>
    <div class="login-wrapper">
        <div class="login-container">
            <div class="login-header">
                <h1>HSR业务系统</h1>
                <p>人力资源管理平台</p>
            </div>
            <div class="login-body">
                <form class="login-form" method="post">
                    <div class="form-group">
                        <label>用户名</label>
                        <div class="input-group">
                            <span class="input-icon">👤</span>
                            <input type="text" name="LoginName" required />
                        </div>
                    </div>
                    <div class="form-group">
                        <label>密码</label>
                        <div class="input-group">
                            <span class="input-icon">🔒</span>
                            <input type="password" name="Password" required />
                        </div>
                    </div>
                    <div class="remember-me">
                        <input type="checkbox" id="remember" name="RememberMe" />
                        <label for="remember">记住我</label>
                    </div>
                    <button type="submit" class="btn-login">登录</button>
                </form>
            </div>
        </div>
    </div>
</body>
</html>
```

### 3. JavaScript功能使用示例

```javascript
// 显示成功消息
showSuccess('保存成功！');

// 显示错误消息
showError('操作失败，请重试');

// 确认删除
if (confirmDelete('确定要删除这条记录吗？')) {
    // 执行删除操作
}

// 表格搜索
<input type="text" id="search" data-search-table="dataTable" />
// 自动绑定搜索功能

// AJAX请求
async function loadData() {
    try {
        const data = await ajaxGet('/api/employees');
        console.log(data);
    } catch (error) {
        showError('加载失败');
    }
}

// 格式化日期
const formattedDate = formatDate(new Date(), 'YYYY-MM-DD HH:mm:ss');

// 导出Excel
function exportData() {
    exportTableToExcel('dataTable', '员工列表');
}
```

## 🎨 CSS变量配置

可以通过修改CSS变量来自定义主题：

```css
:root {
    --primary-color: #3498db;      /* 主色调 */
    --secondary-color: #2c3e50;    /* 次要色 */
    --success-color: #27ae60;      /* 成功色 */
    --warning-color: #f39c12;      /* 警告色 */
    --danger-color: #e74c3c;       /* 危险色 */
    --light-bg: #f8f9fa;          /* 浅色背景 */
    --border-color: #dee2e6;      /* 边框色 */
    --text-color: #333;           /* 文字色 */
    --sidebar-width: 250px;       /* 侧边栏宽度 */
}
```

## 📦 需要您添加的内容

### 1. Logo图片
在 `wwwroot/images/` 目录下添加：
- `logo.png`（系统Logo，建议尺寸：200x60px）

### 2. 第三方库（可选）

如果需要使用第三方库，推荐使用CDN：

```html
<!-- jQuery（如果需要） -->
<script src="https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js"></script>

<!-- Bootstrap（如果需要） -->
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet">

<!-- Font Awesome图标 -->
<link href="https://cdn.jsdelivr.net/npm/@fortawesome/fontawesome-free@6.0.0/css/all.min.css" rel="stylesheet">
```

## ✨ 功能特点

### 完全自包含
- ✅ 不依赖任何第三方库
- ✅ 纯原生JavaScript
- ✅ 纯CSS实现（无预处理器）
- ✅ 完全可定制

### 性能优化
- ✅ 最小化DOM操作
- ✅ 事件委托
- ✅ 防抖/节流
- ✅ 懒加载支持

### 兼容性
- ✅ 现代浏览器（Chrome, Firefox, Safari, Edge）
- ✅ IE11+（需要polyfill）
- ✅ 移动设备

### 可访问性
- ✅ 键盘导航
- ✅ 语义化HTML
- ✅ ARIA标签支持

## 🔧 部署步骤

1. 将整个 `wwwroot` 文件夹复制到项目根目录
2. 确保项目文件中包含：
   ```xml
   <ItemGroup>
     <Content Include="wwwroot\**\*">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </Content>
   </ItemGroup>
   ```
3. 发布时会自动包含所有静态文件

## 📝 注意事项

1. **文件路径**：确保在Razor视图中使用 `~/` 前缀引用静态文件
2. **缓存问题**：更新CSS/JS后可能需要清除浏览器缓存
3. **生产环境**：建议使用CDN或启用静态文件压缩

## 🐛 故障排查

### 样式不生效
1. 检查文件路径是否正确
2. 检查浏览器控制台是否有404错误
3. 确认wwwroot在发布目录中

### JavaScript功能不工作
1. 检查浏览器控制台错误
2. 确认site.js已正确加载
3. 检查函数调用是否在DOM加载后

### 响应式布局问题
1. 检查viewport meta标签
2. 测试不同屏幕尺寸
3. 检查CSS媒体查询

## 📞 技术支持

如有问题，请检查：
1. 浏览器开发者工具（F12）
2. Network标签（检查文件加载）
3. Console标签（检查JavaScript错误）

---

**版本**: 1.0.0  
**最后更新**: 2026-01-11  
**作者**: Claude (Anthropic)

**注意**：这是一个完整的、生产级的前端资源包，包含了所有必要的样式和功能！
