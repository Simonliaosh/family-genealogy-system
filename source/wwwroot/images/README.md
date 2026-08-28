# 图片资源目录

## 需要添加的图片文件

### 1. logo.png（必需）
- **用途**：系统Logo，显示在登录页面和导航栏
- **建议尺寸**：200x60px 或 300x90px
- **格式**：PNG（支持透明背景）
- **示例位置**：
  ```html
  <img src="/images/logo.png" alt="HSR系统" />
  ```

### 2. favicon.ico（推荐）
- **用途**：浏览器标签页图标
- **建议尺寸**：32x32px 或 16x16px
- **格式**：ICO或PNG
- **使用方法**：在 `<head>` 中添加：
  ```html
  <link rel="icon" href="/images/favicon.ico" type="image/x-icon" />
  ```

### 3. 用户默认头像（可选）
- **文件名**：`default-avatar.png`
- **建议尺寸**：128x128px
- **用途**：用户没有上传头像时的默认显示

### 4. 空状态图片（可选）
- **文件名**：`empty-state.svg` 或 `empty-state.png`
- **建议尺寸**：200x200px
- **用途**：表格无数据时的提示图片

## 使用示例

### Logo显示
```html
<div class="sidebar-header">
    <a href="/" class="sidebar-brand">
        <img src="/images/logo.png" alt="HSR" height="40" />
    </a>
</div>
```

### 用户头像
```html
<div class="user-info">
    <img src="/images/default-avatar.png" alt="用户" class="user-avatar" />
</div>
```

### 空状态
```html
<div class="empty-state">
    <img src="/images/empty-state.svg" alt="暂无数据" />
    <p>暂无数据</p>
</div>
```

## 图片优化建议

1. **压缩**：使用TinyPNG等工具压缩图片
2. **格式**：
   - Logo：PNG（透明背景）
   - 照片：JPG
   - 图标：SVG（矢量）
3. **命名**：使用小写字母和连字符，如 `user-avatar.png`
4. **尺寸**：根据实际显示大小提供2倍分辨率（用于高清屏）

## 在线生成工具

- **Logo生成**：https://www.canva.com/
- **Favicon生成**：https://favicon.io/
- **图标**：https://fontawesome.com/
- **插图**：https://undraw.co/

---

**注意**：此目录中的图片将在发布时自动包含到输出目录。
