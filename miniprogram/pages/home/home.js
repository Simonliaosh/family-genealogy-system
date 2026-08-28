const api = require('../../utils/request');

Page({
  data: { name: '' },
  onShow() {
    if (!wx.getStorageSync('ft_token')) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    this.setData({ name: wx.getStorageSync('ft_name') || '族人' });
  },
  go(e) { wx.navigateTo({ url: e.currentTarget.dataset.url }); },
  goTab(e) { wx.switchTab({ url: e.currentTarget.dataset.url }); },
  logout() {
    api.logout();
    wx.reLaunch({ url: '/pages/login/login' });
  }
});
