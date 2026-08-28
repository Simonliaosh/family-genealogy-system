// HSR系统 - 前端JavaScript（简化版）

// 注意：_Layout.cshtml底部已经有内联JavaScript处理菜单功能
// 这个文件只保留其他通用功能

// 等待DOM加载完成
document.addEventListener('DOMContentLoaded', function() {
    // 设置当前激活的菜单项
    setActiveMenu();
    
    // 响应式处理
    handleResponsive();
});

// 设置当前激活的菜单项
function setActiveMenu() {
    const currentPath = window.location.pathname;
    const menuLinks = document.querySelectorAll('.sidebar-menu a.menu-link');
    
    menuLinks.forEach(function(link) {
        const href = link.getAttribute('href');
        if (href && href !== '#' && href !== 'javascript:void(0);') {
            // 移除之前的active类
            link.classList.remove('active');
            
            // 检查当前路径是否匹配
            if (currentPath === href || currentPath.startsWith(href + '/')) {
                link.classList.add('active');
                
                // 如果是子菜单，仅展开所属一级菜单，并合上其他二级菜单
                const parentSubmenu = link.closest('.submenu');
                if (parentSubmenu) {
                    const parentMenuItem = parentSubmenu.closest('.menu-item.has-submenu');
                    if (parentMenuItem) {
                        document.querySelectorAll('.menu-item.has-submenu').forEach(function (m) {
                            if (m !== parentMenuItem) m.classList.remove('active');
                        });
                        parentMenuItem.classList.add('active');
                    }
                }
            }
        }
    });
}

// 响应式处理
function handleResponsive() {
    function applyViewportState() {
        const sidebar = document.getElementById('sidebar');
        const isLandscape = window.matchMedia('(orientation: landscape)').matches;
        document.body.classList.toggle('is-landscape', isLandscape);
        document.body.classList.toggle('is-portrait', !isLandscape);

        // 横竖屏切换后，强制触发表格等 sticky 元素重新计算布局
        window.dispatchEvent(new Event('hsr:viewport-changed'));

        if (window.innerWidth > 768) {
            if (sidebar && sidebar.classList.contains('active')) {
                sidebar.classList.remove('active');
            }
        }
    }

    window.addEventListener('resize', applyViewportState);
    window.addEventListener('orientationchange', function () {
        // 部分移动浏览器在旋转后需要一个短延时才能拿到稳定尺寸
        setTimeout(applyViewportState, 120);
    });

    applyViewportState();
}

// 工具函数：显示加载中
function showLoading() {
    console.log('加载中...');
}

// 工具函数：隐藏加载中
function hideLoading() {
    console.log('加载完成');
}

// 工具函数：显示提示消息
function showMessage(message, type = 'info') {
    // type: success, error, warning, info
    // 这里可以集成更好的提示组件，比如 toastr 或 sweetalert
    console.log(`[${type.toUpperCase()}] ${message}`);
    alert(message);
}

// 工具函数：确认对话框
function confirmAction(message, callback) {
    if (confirm(message)) {
        if (typeof callback === 'function') {
            callback();
        }
        return true;
    }
    return false;
}

// 工具函数：格式化日期
function formatDate(date, format = 'YYYY-MM-DD') {
    if (!(date instanceof Date)) {
        date = new Date(date);
    }
    
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hour = String(date.getHours()).padStart(2, '0');
    const minute = String(date.getMinutes()).padStart(2, '0');
    const second = String(date.getSeconds()).padStart(2, '0');
    
    return format
        .replace('YYYY', year)
        .replace('MM', month)
        .replace('DD', day)
        .replace('HH', hour)
        .replace('mm', minute)
        .replace('ss', second);
}

// 族谱树展开/收起（挂在 document 上，侧栏 AJAX 换页后仍可用）
(function bindFtTreeToggle() {
    if (window.__ftTreeToggleBound) return;
    window.__ftTreeToggleBound = true;

    function childByClass(el, cls) {
        if (!el) return null;
        for (var i = 0; i < el.children.length; i++) {
            if (el.children[i].classList.contains(cls)) return el.children[i];
        }
        return null;
    }

    function setExpanded(li, open) {
        if (!li || !li.classList.contains('ft-tree-node')) return;
        var btn = childByClass(li, 'ft-tree-toggle');
        if (!btn) return;
        li.classList.toggle('collapsed', !open);
        var kids = childByClass(li, 'ft-tree-children');
        if (kids) kids.hidden = !open;
        btn.setAttribute('aria-expanded', open ? 'true' : 'false');
        btn.setAttribute('title', open ? '收起' : '展开');
        btn.setAttribute('aria-label', open ? '收起下级' : '展开下级');
        btn.textContent = open ? '−' : '+';
    }

    document.addEventListener('click', function (e) {
        var t = e.target;
        if (!t || !t.closest) return;

        var toggle = t.closest('.ft-tree-toggle');
        if (toggle) {
            e.preventDefault();
            var li = toggle.closest('li.ft-tree-node');
            setExpanded(li, li.classList.contains('collapsed'));
            return;
        }
        if (t.closest('.ft-tree-expand-all')) {
            e.preventDefault();
            document.querySelectorAll('.ft-tree-root li.ft-tree-node').forEach(function (li) {
                setExpanded(li, true);
            });
            return;
        }
        if (t.closest('.ft-tree-collapse-all')) {
            e.preventDefault();
            document.querySelectorAll('.ft-tree-root li.ft-tree-node').forEach(function (li) {
                setExpanded(li, false);
            });
        }
    });
})();

// 导出工具函数（如果需要）
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        showLoading,
        hideLoading,
        showMessage,
        confirmAction,
        formatDate
    };
}
