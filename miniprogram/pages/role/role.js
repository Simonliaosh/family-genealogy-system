const api = require('../../utils/request');

Page({
  data: {
    status: {},
    rows: [],
    reason: ''
  },
  onShow() {
    if (!wx.getStorageSync('ft_token')) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    this.load();
  },
  load() {
    api.get('/api/FtBranchApply').then(res => {
      const d = res.data || {};
      this.setData({ status: d.status || {}, rows: d.rows || [] });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  onReason(e) { this.setData({ reason: e.detail.value }); },
  submit() {
    wx.showLoading({ title: '提交中' });
    api.post('/api/FtBranchApply', { reason: this.data.reason }).then(res => {
      wx.hideLoading();
      wx.showToast({ title: res.message || '已提交' });
      this.setData({ reason: '' });
      this.load();
    }).catch(err => {
      wx.hideLoading();
      wx.showToast({ title: err.message, icon: 'none' });
    });
  }
});
