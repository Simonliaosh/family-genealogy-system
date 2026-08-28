(function () {
    'use strict';

    const api = {
        getLayout: '/EDashBoard/GetLayout',
        getIndicatorData: '/EDashBoard/GetIndicatorData',
        getAvailableIndicators: '/EDashBoard/GetAvailableIndicators',
        switchUserPos: '/EDashBoard/SwitchUserPos',
        resetToPosDefault: '/EDashBoard/ResetToPosDefault',
        addCard: '/EDashBoard/AddCard',
        saveGlobalFilter: '/EDashBoard/SaveGlobalFilter',
        deleteCard: '/EDashBoard/DeleteCard'
    };

    let layout = null;
    let editMode = false;

    const gridEl = document.getElementById('dash-grid');
    const posSelect = document.getElementById('dash-user-pos');
    const timeTypeSelect = document.getElementById('dash-time-type');
    const loadingEl = document.getElementById('dash-loading');
    const errorEl = document.getElementById('dash-error');

    function getToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    async function postJson(url, body) {
        const res = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getToken()
            },
            body: JSON.stringify(body)
        });
        return res.json();
    }

    async function getJson(url) {
        const res = await fetch(url);
        return res.json();
    }

    function showLoading(show) {
        if (loadingEl) loadingEl.classList.toggle('d-none', !show);
    }

    function showError(msg) {
        if (!errorEl) return;
        if (msg) {
            errorEl.textContent = msg;
            errorEl.classList.remove('d-none');
        } else {
            errorEl.textContent = '';
            errorEl.classList.add('d-none');
        }
    }

    function getTimeType() {
        return parseInt(timeTypeSelect?.value || '3', 10);
    }

    function getGlobalFilter() {
        return {
            timeType: getTimeType(),
            deptId: layout?.globalFilter?.deptId ?? 0
        };
    }

    function disposeCardChart(bodyEl) {
        if (window.DashCharts && bodyEl) {
            DashCharts.dispose(bodyEl);
        }
    }

    function renderKpi(container, data) {
        const value = data?.value ?? data?.Value ?? '-';
        const unit = data?.unit ?? data?.Unit ?? '';
        const trend = data?.trend ?? data?.Trend ?? '';
        const changeRate = data?.changeRate ?? data?.ChangeRate;
        const finishRate = data?.finishRate ?? data?.FinishRate;
        const targetValue = data?.targetValue ?? data?.TargetValue;

        container.innerHTML =
            '<div class="dash-kpi">' +
            '<div class="dash-kpi-value">' + escapeHtml(String(value)) +
            (unit ? '<span class="dash-kpi-unit">' + escapeHtml(unit) + '</span>' : '') +
            '</div>' +
            (finishRate != null ? '<div class="dash-kpi-meta">完成率 ' + escapeHtml(String(finishRate)) + '%</div>' : '') +
            (targetValue != null ? '<div class="dash-kpi-meta">目标 ' + escapeHtml(String(targetValue)) + '</div>' : '') +
            (changeRate != null ? '<div class="dash-kpi-meta">环比 ' + escapeHtml(String(changeRate)) + '%</div>' : '') +
            (trend ? '<div class="dash-kpi-meta">趋势 ' + escapeHtml(trend) + '</div>' : '') +
            '</div>';
    }

    function renderQuickMenu(container, data) {
        const menus = data?.menuList ?? data?.MenuList ?? [];
        if (!menus.length) {
            container.innerHTML = '<div class="dash-card-loading">暂无菜单项</div>';
            return;
        }
        let html = '<div class="dash-menu-grid">';
        menus.forEach(function (m) {
            const icon = m.icon || m.Icon || '●';
            const name = m.name || m.Name || '';
            const route = m.routePath || m.RoutePath || '#';
            const bg = m.bgColor || m.BgColor || '#409eff';
            html += '<a class="dash-menu-item" href="' + escapeAttr(route) + '">' +
                '<span class="dash-menu-icon" style="background:' + escapeAttr(bg) + '">' + escapeHtml(icon) + '</span>' +
                '<span class="dash-menu-name">' + escapeHtml(name) + '</span></a>';
        });
        html += '</div>';
        container.innerHTML = html;
    }

    function renderScrollList(container, data) {
        const items = data?.list ?? data?.List ?? [];
        if (!items.length) {
            container.innerHTML = '<div class="dash-card-loading">暂无数据</div>';
            return;
        }
        let html = '<div class="dash-scroll-list">';
        items.forEach(function (item) {
            const num = item.sortNum ?? item.SortNum ?? '';
            const title = item.title ?? item.Title ?? '';
            const remark = item.remark ?? item.Remark ?? '';
            const time = item.createTime ?? item.CreateTime ?? '';
            const route = item.itemRoute ?? item.ItemRoute ?? '#';
            html += '<div class="dash-scroll-item">' +
                '<span class="dash-scroll-num">' + escapeHtml(String(num)) + '</span>' +
                '<div><a href="' + escapeAttr(route) + '">' + escapeHtml(title) + '</a>' +
                '<div class="dash-scroll-meta">' + escapeHtml(remark) +
                (time ? ' · ' + escapeHtml(time) : '') + '</div></div></div>';
        });
        html += '</div>';
        container.innerHTML = html;
    }

    function renderCardContent(bodyEl, chartType, data) {
        disposeCardChart(bodyEl);
        const ct = parseInt(chartType || data?.chartType || data?.ChartType || 1, 10);

        switch (ct) {
            case 1:
                renderKpi(bodyEl, data);
                break;
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
                if (window.DashCharts) {
                    DashCharts.render(bodyEl, ct, data);
                } else {
                    bodyEl.innerHTML = '<div class="dash-card-error">图表组件未加载</div>';
                }
                break;
            case 8:
                renderQuickMenu(bodyEl, data);
                break;
            case 9:
                renderScrollList(bodyEl, data);
                break;
            default:
                bodyEl.innerHTML = '<div class="dash-card-loading">图表类型 ' + ct + ' 暂未实现</div>';
        }
    }

    async function loadCardData(card, bodyEl) {
        bodyEl.innerHTML = '<div class="dash-card-loading">加载中…</div>';
        try {
            const result = await postJson(api.getIndicatorData, {
                indicatorId: card.indicatorId,
                cardId: card.cardId,
                globalFilter: getGlobalFilter(),
                cardFilterJson: card.userFilterJson || null
            });
            if (result.code === 204) {
                bodyEl.innerHTML = '<div class="dash-card-loading">' + escapeHtml(result.msg || '暂无数据') + '</div>';
                return;
            }
            if (result.code !== 200) {
                bodyEl.innerHTML = '<div class="dash-card-error">' + escapeHtml(result.msg || '加载失败') + '</div>';
                return;
            }
            const data = result.data;
            const chartType = card.chartType || (data && !Array.isArray(data) ? (data.chartType || data.ChartType) : null);
            renderCardContent(bodyEl, chartType, data);
            if (window.DashCharts) {
                setTimeout(function () { DashCharts.resizeAll(); }, 50);
            }
        } catch (e) {
            bodyEl.innerHTML = '<div class="dash-card-error">请求异常</div>';
        }
    }

    function buildCardElement(card) {
        const el = document.createElement('div');
        el.className = 'dash-card';
        el.dataset.cardId = card.cardId;
        el.style.gridColumn = (card.layoutCol || 1) + ' / span ' + (card.colSpan || 1);
        el.style.gridRow = String(card.layoutRow || 'auto');

        const header = document.createElement('div');
        header.className = 'dash-card-header';
        header.innerHTML = '<span class="dash-card-title">' + escapeHtml(card.cardTitle || card.indicatorCode || '') + '</span>';

        const actions = document.createElement('div');
        actions.className = 'dash-card-actions';
        const delBtn = document.createElement('button');
        delBtn.type = 'button';
        delBtn.className = 'btn btn-sm btn-outline-danger';
        delBtn.textContent = '移除';
        delBtn.addEventListener('click', function () { removeCard(card.cardId); });
        actions.appendChild(delBtn);
        header.appendChild(actions);

        const body = document.createElement('div');
        body.className = 'dash-card-body';

        el.appendChild(header);
        el.appendChild(body);

        if (!card.isHide) {
            loadCardData(card, body);
        } else {
            body.innerHTML = '<div class="dash-card-loading">已隐藏</div>';
        }

        return el;
    }

    function renderGrid() {
        if (!gridEl || !layout) return;
        if (window.DashCharts) {
            gridEl.querySelectorAll('.dash-card-body').forEach(function (body) {
                DashCharts.dispose(body);
            });
        }
        gridEl.innerHTML = '';
        gridEl.classList.toggle('edit-mode', editMode);

        const cards = (layout.cards || []).filter(function (c) { return !c.isHide; });
        cards.sort(function (a, b) {
            if (a.layoutRow !== b.layoutRow) return a.layoutRow - b.layoutRow;
            return a.layoutCol - b.layoutCol;
        });

        cards.forEach(function (card) {
            gridEl.appendChild(buildCardElement(card));
        });

        if (cards.length === 0) {
            gridEl.innerHTML = '<div class="text-muted small">暂无卡片，可点击「添加卡片」或「恢复默认」。</div>';
        }
    }

    function fillPosSelect() {
        if (!posSelect || !layout) return;
        posSelect.innerHTML = '';
        (layout.posList || []).forEach(function (p) {
            const opt = document.createElement('option');
            opt.value = p.userPosId;
            const label = (p.posName || '') + (p.deptName ? ' · ' + p.deptName : '');
            opt.textContent = label + (p.isPrimary ? ' (主岗)' : '');
            if (p.isCurrent || p.userPosId === layout.currentUserPosId) opt.selected = true;
            posSelect.appendChild(opt);
        });
    }

    async function loadLayout() {
        showLoading(true);
        showError('');
        try {
            const result = await getJson(api.getLayout);
            if (result.code !== 200) {
                showError(result.msg || '加载布局失败');
                return;
            }
            layout = result.data;
            if (layout?.globalFilter?.timeType && timeTypeSelect) {
                timeTypeSelect.value = String(layout.globalFilter.timeType);
            }
            fillPosSelect();
            renderGrid();
        } catch (e) {
            showError('加载布局异常');
        } finally {
            showLoading(false);
        }
    }

    async function refreshAll() {
        await postJson(api.saveGlobalFilter, getGlobalFilter());
        if (!gridEl) return;
        gridEl.querySelectorAll('.dash-card').forEach(function (cardEl) {
            const cardId = parseInt(cardEl.dataset.cardId, 10);
            const card = (layout?.cards || []).find(function (c) { return c.cardId === cardId; });
            const body = cardEl.querySelector('.dash-card-body');
            if (card && body) loadCardData(card, body);
        });
    }

    async function switchPos(userPosId) {
        showLoading(true);
        try {
            const result = await postJson(api.switchUserPos, { userPosId: userPosId });
            if (result.code !== 200) {
                showError(result.msg || '切换任岗失败');
                return;
            }
            layout = result.data;
            fillPosSelect();
            renderGrid();
        } catch (e) {
            showError('切换任岗异常');
        } finally {
            showLoading(false);
        }
    }

    async function resetToDefault() {
        if (!confirm('确定恢复为岗位默认布局？当前自定义卡片将被重置。')) return;
        showLoading(true);
        try {
            const result = await postJson(api.resetToPosDefault, {});
            if (result.code !== 200) {
                showError(result.msg || '恢复失败');
                return;
            }
            layout = result.data;
            fillPosSelect();
            renderGrid();
        } catch (e) {
            showError('恢复默认异常');
        } finally {
            showLoading(false);
        }
    }

    async function addCard() {
        const indResult = await getJson(api.getAvailableIndicators);
        if (indResult.code !== 200 || !indResult.data?.length) {
            alert('暂无可用指标');
            return;
        }
        const indicators = indResult.data;
        let msg = '请选择指标编号：\n';
        indicators.forEach(function (ind, i) {
            msg += (i + 1) + '. ' + ind.indicatorCode + ' - ' + ind.indicatorName + ' (' + (ind.chartTypeName || ind.chartType) + ')\n';
        });
        const input = prompt(msg, '1');
        if (!input) return;
        const idx = parseInt(input, 10) - 1;
        const ind = indicators[idx];
        if (!ind) {
            alert('无效选择');
            return;
        }
        const title = prompt('卡片标题', ind.indicatorName || ind.indicatorCode);
        if (!title) return;

        const result = await postJson(api.addCard, {
            indicatorId: ind.dataId,
            cardTitle: title.trim(),
            layoutRow: 99,
            layoutCol: 1,
            colSpan: 2
        });
        if (result.code !== 200) {
            alert(result.msg || '添加失败');
            return;
        }
        await loadLayout();
    }

    async function removeCard(cardId) {
        if (!confirm('确定移除此卡片？')) return;
        const result = await postJson(api.deleteCard, { cardId: cardId });
        if (result.code !== 200) {
            alert(result.msg || '移除失败');
            return;
        }
        await loadLayout();
    }

    function escapeHtml(s) {
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function escapeAttr(s) {
        return escapeHtml(s).replace(/'/g, '&#39;');
    }

    function bindEvents() {
        document.getElementById('btn-dash-refresh')?.addEventListener('click', refreshAll);
        document.getElementById('btn-dash-add')?.addEventListener('click', addCard);
        document.getElementById('btn-dash-reset')?.addEventListener('click', resetToDefault);
        document.getElementById('btn-dash-edit')?.addEventListener('click', function () {
            editMode = !editMode;
            gridEl?.classList.toggle('edit-mode', editMode);
            this.classList.toggle('btn-secondary', editMode);
            this.classList.toggle('btn-outline-secondary', !editMode);
            this.textContent = editMode ? '退出编辑' : '编辑模式';
        });
        posSelect?.addEventListener('change', function () {
            switchPos(parseInt(this.value, 10));
        });
        timeTypeSelect?.addEventListener('change', function () {
            refreshAll();
        });
        if (!window._dashBoardResizeBound) {
            window._dashBoardResizeBound = true;
            window.addEventListener('resize', function () {
                if (window.DashCharts) DashCharts.resizeAll();
            });
        }
    }

    function destroy() {
        if (gridEl && window.DashCharts) {
            gridEl.querySelectorAll('.dash-card-body').forEach(function (body) {
                DashCharts.dispose(body);
            });
        }
        layout = null;
        editMode = false;
        const page = document.getElementById('dash-board-page');
        if (page) delete page.dataset.initialized;
    }

    function init() {
        const page = document.getElementById('dash-board-page');
        if (!page || page.dataset.initialized === '1') return;
        page.dataset.initialized = '1';
        bindEvents();
        loadLayout();
    }

    window.DashBoardPage = {
        init: init,
        destroy: destroy
    };

    if (document.getElementById('dash-board-page')) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', init);
        }
    }
})();
