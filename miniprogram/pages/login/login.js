const api = require('../../utils/request');

Page({
  data: {
    wechatEnabled: false,
    allowDevMock: false,
    idCard: '',
    password: '',
    realName: '',
    regId: '',
    regPwd: '',
    regPwd2: ''
  },
  onShow() {
    if (wx.getStorageSync('ft_token')) {
      wx.switchTab({ url: '/pages/home/home' });
      return;
    }
    api.get('/api/FtAuth/Config').then(res => {
      const d = res.data || {};
      this.setData({ wechatEnabled: !!d.wechatEnabled, allowDevMock: !!d.allowDevMock });
    }).catch(() => {});
  },
  onId(e) { this.setData({ idCard: e.detail.value }); },
  onPwd(e) { this.setData({ password: e.detail.value }); },
  onName(e) { this.setData({ realName: e.detail.value }); },
  onRegId(e) { this.setData({ regId: e.detail.value }); },
  onRegPwd(e) { this.setData({ regPwd: e.detail.value }); },
  onRegPwd2(e) { this.setData({ regPwd2: e.detail.value }); },
  afterLogin() {
    wx.showToast({ title: '已登录' });
    wx.switchTab({ url: '/pages/home/home' });
  },
  onWxLogin() {
    const that = this;
    wx.showLoading({ title: '登录中' });
    wx.login({
      success(r) {
        const body = { code: r.code || '' };
        if (that.data.allowDevMock && !that.data.wechatEnabled) {
          let mock = wx.getStorageSync('ft_mock_openid');
          if (!mock) {
            mock = 'dev:' + Date.now();
            wx.setStorageSync('ft_mock_openid', mock);
          }
          body.mockOpenId = mock;
          body.code = mock;
        }
        api.post('/api/FtAuth/WeChatLogin', body).then(res => {
          wx.hideLoading();
          api.saveLogin(res);
          that.afterLogin();
        }).catch(err => {
          wx.hideLoading();
          wx.showToast({ title: err.message, icon: 'none' });
        });
      },
      fail() {
        wx.hideLoading();
        wx.showToast({ title: 'wx.login 失败', icon: 'none' });
      }
    });
  },
  onIdLogin() {
    wx.showLoading({ title: '登录中' });
    api.post('/api/FtAuth/IdCardLogin', { idCard: this.data.idCard, password: this.data.password })
      .then(res => { wx.hideLoading(); api.saveLogin(res); this.afterLogin(); })
      .catch(err => { wx.hideLoading(); wx.showToast({ title: err.message, icon: 'none' }); });
  },
  onRegister() {
    wx.showLoading({ title: '注册中' });
    api.post('/api/FtAuth/Register', {
      loginName: this.data.regId,
      realName: this.data.realName,
      password: this.data.regPwd,
      password2: this.data.regPwd2
    }).then(res => {
      wx.hideLoading();
      api.saveLogin(res);
      this.afterLogin();
    }).catch(err => {
      wx.hideLoading();
      wx.showToast({ title: err.message, icon: 'none' });
    });
  }
});
