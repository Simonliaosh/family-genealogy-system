const api = require('../../utils/request');

Page({
  data: { todos: [], links: [] },
  onShow() {
    if (!wx.getStorageSync('ft_token')) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    api.get('/api/FtMessage').then(res => {
      const d = res.data || {};
      this.setData({ todos: d.todos || [], links: d.links || [] });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  }
});
