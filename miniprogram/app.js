App({
  onLaunch() {
    const token = wx.getStorageSync('ft_token');
    if (!token) return;
  }
});
