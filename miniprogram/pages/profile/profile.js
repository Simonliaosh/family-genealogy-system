const api = require('../../utils/request');

Page({
  data: {
    bound: true,
    personId: 0,
    fullName: '',
    nickName: '',
    selfIntro: '',
    wechatId: '',
    showPhoto: false,
    showWechat: false,
    printAllow: true,
    photos: [],
    bindId: ''
  },
  onShow() {
    if (!wx.getStorageSync('ft_token')) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    this.load();
  },
  load() {
    api.get('/api/FtProfile').then(res => {
      const d = res.data || {};
      if (!d.bound) {
        this.setData({ bound: false });
        return;
      }
      this.setData({
        bound: true,
        personId: d.personId,
        fullName: d.fullName || '',
        nickName: d.nickName || '',
        selfIntro: d.selfIntro || '',
        wechatId: d.wechatId || '',
        showPhoto: !!d.showPhoto,
        showWechat: !!d.showWechat,
        printAllow: d.printAllow !== false,
        photos: (d.photos || []).map(api.absUrl)
      });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  set(e) { this.setData({ [e.currentTarget.dataset.k]: e.detail.value }); },
  onSwitch(e) { this.setData({ [e.currentTarget.dataset.k]: e.detail.value }); },
  save() {
    api.post('/api/FtProfile', {
      personId: this.data.personId,
      nickName: this.data.nickName,
      selfIntro: this.data.selfIntro,
      wechatId: this.data.wechatId,
      showPhoto: this.data.showPhoto,
      showWechat: this.data.showWechat,
      printAllow: this.data.printAllow
    }).then(res => {
      wx.showToast({ title: res.message || '已保存' });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  choosePhoto() {
    const that = this;
    wx.chooseImage({
      count: 1,
      sizeType: ['compressed'],
      success(r) {
        const path = r.tempFilePaths[0];
        wx.showLoading({ title: '上传中' });
        api.uploadPhoto(path, that.data.personId).then(() => {
          wx.hideLoading();
          wx.showToast({ title: '已上传' });
          that.load();
        }).catch(err => {
          wx.hideLoading();
          wx.showToast({ title: err.message, icon: 'none' });
        });
      }
    });
  },
  delPhoto(e) {
    const path = e.currentTarget.dataset.path || '';
    const rel = path.replace(/^https?:\/\/[^/]+/i, '');
    wx.showModal({
      title: '删除这张照片？',
      success: (r) => {
        if (!r.confirm) return;
        api.post('/api/FtProfile/DeletePhoto', { personId: this.data.personId, path: rel })
          .then(() => this.load())
          .catch(err => wx.showToast({ title: err.message, icon: 'none' }));
      }
    });
  },
  bindIdCard() {
    api.post('/api/FtAuth/BindIdCard', { idCard: this.data.bindId })
      .then(res => wx.showToast({ title: res.message || '已绑定' }))
      .catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  }
});
