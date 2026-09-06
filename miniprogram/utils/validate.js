/**
 * 客户端校验。原先全应用零校验：占位符写着「18位身份证」「至少6位」「再输一次密码」，
 * 而代码原样提交、从不比较两次口令——打错一个字符要一次网络往返才知道。
 */

/** 18 位身份证：格式 + 校验位（GB 11643-1999）。 */
function isIdCard(raw) {
  const s = (raw || '').trim().toUpperCase();
  if (!/^\d{17}[\dX]$/.test(s)) return false;
  const weights = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];
  const checks = ['1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2'];
  let sum = 0;
  for (let i = 0; i < 17; i++) sum += parseInt(s[i], 10) * weights[i];
  return checks[sum % 11] === s[17];
}

/** 登录名：至少 6 位，只能字母或数字（手机号、拼音都满足）。 */
function isLoginName(raw) {
  return /^[A-Za-z0-9]{6,32}$/.test((raw || '').trim());
}

/** 口令：至少 6 位。 */
function isPassword(raw) {
  return (raw || '').trim().length >= 6;
}

/** 出生日期：YYYY-MM-DD，且是真实存在的日期。允许留空。 */
function isBirthDate(raw) {
  const s = (raw || '').trim();
  if (s.length === 0) return true;
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(s);
  if (!m) return false;
  const y = +m[1], mo = +m[2], d = +m[3];
  if (mo < 1 || mo > 12 || d < 1 || d > 31) return false;
  const dt = new Date(y, mo - 1, d);
  return dt.getFullYear() === y && dt.getMonth() === mo - 1 && dt.getDate() === d;
}

/** 世代号：留空或正整数。原先 parseInt(...) || 0 把非法值静默变成 0。 */
function parseGeneration(raw) {
  const s = (raw || '').trim();
  if (s.length === 0) return { ok: true, value: 0 };
  if (!/^\d{1,3}$/.test(s)) return { ok: false, value: 0, message: '世代号请填 1~999 的整数' };
  return { ok: true, value: parseInt(s, 10) };
}

/** 逐条跑校验，返回第一条失败信息；全部通过返回 null。 */
function firstError(rules) {
  for (let i = 0; i < rules.length; i++) {
    if (!rules[i].ok) return rules[i].message;
  }
  return null;
}

module.exports = { isIdCard, isLoginName, isPassword, isBirthDate, parseGeneration, firstError };
