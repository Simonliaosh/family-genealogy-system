const api = require('./request');

/**
 * 页面公共部分。
 *
 * 原先每个页面各抄一份 `if (!wx.getStorageSync('ft_token')) wx.reLaunch(...)`（共 6 处），
 * 并且加载失败后统一只弹一个 toast——页面上显示的仍是「暂无记录」，
 * 与「确实没数据」视觉上不可区分，也没有任何重试入口。
 */

/** 未登录则跳登录页，返回 false 让调用方直接 return。 */
function requireLogin() {
  if (api.hasValidToken()) return true;
  wx.reLaunch({ url: '/pages/login/login' });
  return false;
}

/**
 * 把一次异步加载包成三态（loading / error / ok），并写进页面 data。
 * 页面模板据此区分「加载中」「加载失败（可重试）」「确实没有数据」。
 *
 * @param {object} page  Page 实例（this）
 * @param {Function} loader 返回 Promise 的加载函数
 * @param {object} [opts] { silent: 下拉刷新时不显示整页 loading }
 */
function load(page, loader, opts) {
  const silent = opts && opts.silent;
  page.setData({ loading: !silent, loadError: '' });
  return loader()
    .then((r) => {
      page.setData({ loading: false, loadError: '' });
      return r;
    })
    .catch((err) => {
      page.setData({ loading: false, loadError: (err && err.message) || '加载失败' });
      throw err;
    })
    .finally(() => {
      if (silent) wx.stopPullDownRefresh();
    });
}

/** 页面 data 里三态字段的初值。 */
const loadState = { loading: false, loadError: '' };

module.exports = { requireLogin, load, loadState };
