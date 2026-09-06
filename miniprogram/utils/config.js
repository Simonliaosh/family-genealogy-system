/**
 * 接口根地址：按小程序运行环境自动切换，不需要手改文件。
 *
 * 原先这里是单一的 `const apiBase = 'http://127.0.0.1:5070'`，并在 README 里
 * 指导每位开发者手改它做真机调试——这基本保证了它迟早被改错提交，
 * 而且与 project.config.json 里提交的 urlCheck:false 是个坏组合。
 *
 * 微信生产环境要求 HTTPS 且域名已备案，正式版地址必须是 https。
 */

const ENV_API_BASE = {
  // 开发者工具 / 本机调试。真机预览请改成电脑的局域网地址（仅本地生效，不要提交）。
  develop: 'http://127.0.0.1:5070',
  // 体验版
  trial: 'https://staging.example.com',
  // 正式版：必须是已备案的 HTTPS 域名
  release: 'https://example.com'
};

function currentEnv() {
  try {
    if (typeof wx !== 'undefined' && wx.getAccountInfoSync) {
      const info = wx.getAccountInfoSync();
      const v = info && info.miniProgram && info.miniProgram.envVersion;
      if (v && ENV_API_BASE[v]) return v;
    }
  } catch (e) {
    // getAccountInfoSync 在极旧基础库上可能不存在，回落到 develop
  }
  return 'develop';
}

const apiBase = ENV_API_BASE[currentEnv()];

/** 请求超时（毫秒）。不设的话继承 60 秒默认值，后端一挂 UI 就冻结整整一分钟。 */
const TIMEOUT_MS = 12000;

/** 幂等的 GET 失败后自动重试次数。 */
const RETRY_COUNT = 2;

module.exports = { apiBase, TIMEOUT_MS, RETRY_COUNT, envVersion: currentEnv() };
