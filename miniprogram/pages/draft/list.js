const api = require('../../utils/request');
const page = require('../../utils/page');

Page({
  data: Object.assign({ rows: [] }, page.loadState),

  onShow() {
    if (!page.requireLogin()) return;
    this.load();
  },

  /** 原先只能靠切走再切回刷新，现在支持下拉。 */
  onPullDownRefresh() {
    this.load({ silent: true });
  },

  load(opts) {
    return page.load(this, () =>
      api.get('/api/FtDraft').then(res => {
        const d = res.data || {};
        this.setData({ rows: d.rows || [] });
      }), opts).catch(() => { /* 错误已写进 loadError，模板负责展示 */ });
  },

  onRetry() { this.load(); },
  create() { wx.navigateTo({ url: '/pages/draft/edit' }); },
  edit(e) { wx.navigateTo({ url: '/pages/draft/edit?id=' + e.currentTarget.dataset.id }); }
});
