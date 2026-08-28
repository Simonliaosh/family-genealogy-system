const api = require('../../utils/request');

Page({
  data: { rows: [] },
  onShow() {
    if (!wx.getStorageSync('ft_token')) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    this.load();
  },
  load() {
    api.get('/api/FtDraft').then(res => {
      const d = res.data || {};
      this.setData({ rows: d.rows || [] });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  create() { wx.navigateTo({ url: '/pages/draft/edit' }); },
  edit(e) { wx.navigateTo({ url: '/pages/draft/edit?id=' + e.currentTarget.dataset.id }); }
});
