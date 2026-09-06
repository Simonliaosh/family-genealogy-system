const api = require('../../utils/request');
const page = require('../../utils/page');

Page({
  data: Object.assign({ todos: [], links: [] }, page.loadState),

  onShow() {
    if (!page.requireLogin()) return;
    this.load();
  },

  onPullDownRefresh() { this.load({ silent: true }); },

  load(opts) {
    return page.load(this, () =>
      api.get('/api/FtMessage').then(res => {
        const d = res.data || {};
        this.setData({ todos: d.todos || [], links: d.links || [] });
      }), opts).catch(() => {});
  },

  onRetry() { this.load(); }
});
