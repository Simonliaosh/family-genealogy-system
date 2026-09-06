const api = require('./utils/request');
const { envVersion, apiBase } = require('./utils/config');

/**
 * 原先 app.js 全文只有 6 行：读了 token 然后两个分支都什么都不做，
 * 也没有 globalData——不是「globalData 一锅粥」，而是什么都没有。
 * 这里放会话状态与全局错误兜底，页面不必各自重复。
 */
App({
  globalData: {
    envVersion,
    apiBase,
    /** 当前用户显示名，登录后由 request.saveLogin 写入 storage，这里做一次缓存 */
    realName: '',
    /** 是否还需要补绑身份证 */
    needIdCard: false
  },

  onLaunch() {
    this.syncSession();
  },

  onShow() {
    this.syncSession();
  },

  /** 把 storage 里的会话状态同步进 globalData；令牌过期就清干净。 */
  syncSession() {
    if (!api.hasValidToken()) {
      api.logout();
      this.globalData.realName = '';
      this.globalData.needIdCard = false;
      return;
    }
    this.globalData.realName = wx.getStorageSync('ft_name') || '';
    this.globalData.needIdCard = !!wx.getStorageSync('ft_needIdCard');
  },

  /** 未捕获的 Promise 拒绝不要静默消失。 */
  onUnhandledRejection(res) {
    const msg = (res && res.reason && res.reason.message) || '发生未知错误';
    console.error('[unhandledRejection]', msg);
  }
});
