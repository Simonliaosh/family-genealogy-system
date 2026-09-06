const api = require('../../utils/request');
const page = require('../../utils/page');

Page({
  data: { name: '' },
  onShow() {
    if (!page.requireLogin()) return;
    this.setData({ name: wx.getStorageSync('ft_name') || '族人' });
  },
  go(e) { wx.navigateTo({ url: e.currentTarget.dataset.url }); },
  goTab(e) { wx.switchTab({ url: e.currentTarget.dataset.url }); },
  logout() {
    api.logout();
    wx.reLaunch({ url: '/pages/login/login' });
  }
});
