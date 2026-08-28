(function (global) {
    'use strict';

    var chartMap = new WeakMap();

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

    function dispose(container) {
        if (!container) return;
        var inst = chartMap.get(container);
        if (inst) {
            inst.dispose();
            chartMap.delete(container);
        }
    }

    function ensureCanvas(container, className) {
        dispose(container);
        container.innerHTML = '';
        var wrap = document.createElement('div');
        wrap.className = className || 'dash-chart-canvas';
        container.appendChild(wrap);
        return wrap;
    }

    function initChart(container, option) {
        if (typeof echarts === 'undefined') {
            container.innerHTML = '<div class="dash-card-error">未加载 ECharts</div>';
            return null;
        }
        var inst = echarts.init(container);
        inst.setOption(option, true);
        chartMap.set(container.parentElement || container, inst);
        return inst;
    }

    function normalizeSeries(data) {
        var series = data?.series ?? data?.Series ?? [];
        if (!Array.isArray(series)) return [];
        return series.map(function (s) {
            return {
                name: s.seriesName ?? s.SeriesName ?? s.name ?? s.Name ?? '',
                data: s.data ?? s.Data ?? []
            };
        });
    }

    function normalizeAxis(data) {
        return data?.xAxis ?? data?.XAxis ?? [];
    }

    function normalizePieItems(data) {
        if (Array.isArray(data)) return data;
        var items = data?.items ?? data?.Items ?? data?.data ?? data?.Data;
        if (Array.isArray(items)) return items;
        return [];
    }

    function renderLine(container, data) {
        var el = ensureCanvas(container, 'dash-chart-canvas dash-chart-line');
        var xAxis = normalizeAxis(data);
        var series = normalizeSeries(data);
        if (!xAxis.length || !series.length) {
            el.innerHTML = '<div class="dash-card-loading">暂无数据</div>';
            return;
        }
        initChart(el, {
            color: ['#409eff', '#67c23a', '#e6a23c', '#f56c6c'],
            tooltip: { trigger: 'axis' },
            legend: { bottom: 0, type: 'scroll' },
            grid: { left: 40, right: 16, top: 24, bottom: 48 },
            xAxis: { type: 'category', data: xAxis, boundaryGap: false },
            yAxis: { type: 'value', minInterval: 1 },
            series: series.map(function (s) {
                return {
                    name: s.name,
                    type: 'line',
                    smooth: true,
                    data: s.data
                };
            })
        });
    }

    function renderBar(container, data) {
        var el = ensureCanvas(container, 'dash-chart-canvas dash-chart-bar');
        var xAxis = normalizeAxis(data);
        var series = normalizeSeries(data);
        if (!xAxis.length || !series.length) {
            el.innerHTML = '<div class="dash-card-loading">暂无数据</div>';
            return;
        }
        initChart(el, {
            color: ['#409eff', '#67c23a', '#e6a23c'],
            tooltip: { trigger: 'axis' },
            legend: { bottom: 0, type: 'scroll' },
            grid: { left: 40, right: 16, top: 24, bottom: 48 },
            xAxis: { type: 'category', data: xAxis },
            yAxis: { type: 'value', minInterval: 1 },
            series: series.map(function (s) {
                return {
                    name: s.name,
                    type: 'bar',
                    barMaxWidth: 36,
                    data: s.data
                };
            })
        });
    }

    function renderPie(container, data) {
        var el = ensureCanvas(container, 'dash-chart-canvas dash-chart-pie');
        var items = normalizePieItems(data).map(function (x) {
            return {
                name: x.name ?? x.Name ?? '',
                value: x.value ?? x.Value ?? 0
            };
        }).filter(function (x) { return x.name; });

        if (!items.length) {
            el.innerHTML = '<div class="dash-card-loading">暂无数据</div>';
            return;
        }
        initChart(el, {
            color: ['#409eff', '#67c23a', '#e6a23c', '#f56c6c', '#909399', '#9b59b6'],
            tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
            legend: { orient: 'vertical', left: 'left', top: 'middle', type: 'scroll' },
            series: [{
                type: 'pie',
                radius: ['42%', '68%'],
                center: ['58%', '50%'],
                avoidLabelOverlap: true,
                itemStyle: { borderRadius: 4, borderColor: '#fff', borderWidth: 1 },
                label: { formatter: '{b}\n{d}%' },
                data: items
            }]
        });
    }

    function renderTable(container, data) {
        dispose(container);
        var columns = data?.columns ?? data?.Columns ?? [];
        var rows = data?.tableData ?? data?.TableData ?? [];
        var pageInfo = data?.pageInfo ?? data?.PageInfo;

        if (!columns.length) {
            container.innerHTML = '<div class="dash-card-loading">暂无列定义</div>';
            return;
        }

        var html = '<div class="dash-table-wrap"><table class="dash-table"><thead><tr>';
        columns.forEach(function (col) {
            var title = col.title ?? col.Title ?? col.key ?? col.Key ?? '';
            html += '<th>' + escapeHtml(title) + '</th>';
        });
        html += '</tr></thead><tbody>';

        if (!rows.length) {
            html += '<tr><td colspan="' + columns.length + '" class="text-center text-muted">暂无数据</td></tr>';
        } else {
            rows.forEach(function (row) {
                html += '<tr>';
                columns.forEach(function (col) {
                    var key = col.key ?? col.Key ?? '';
                    var val = row[key];
                    if (val == null) val = '';
                    html += '<td>' + escapeHtml(String(val)) + '</td>';
                });
                html += '</tr>';
            });
        }
        html += '</tbody></table>';

        if (pageInfo) {
            var total = pageInfo.total ?? pageInfo.Total ?? rows.length;
            var current = pageInfo.current ?? pageInfo.Current ?? 1;
            var pageSize = pageInfo.pageSize ?? pageInfo.PageSize ?? rows.length;
            html += '<div class="dash-table-pager">共 ' + escapeHtml(String(total)) +
                ' 条 · 第 ' + escapeHtml(String(current)) + ' 页 · 每页 ' + escapeHtml(String(pageSize)) + ' 条</div>';
        }
        html += '</div>';
        container.innerHTML = html;
    }

    function renderFunnel(container, data) {
        var el = ensureCanvas(container, 'dash-chart-canvas dash-chart-funnel');
        var items = normalizePieItems(data).map(function (x) {
            return {
                name: x.name ?? x.Name ?? '',
                value: x.value ?? x.Value ?? 0
            };
        }).filter(function (x) { return x.name; });

        if (!items.length) {
            el.innerHTML = '<div class="dash-card-loading">暂无数据</div>';
            return;
        }

        initChart(el, {
            color: ['#409eff', '#67c23a', '#e6a23c', '#f56c6c'],
            tooltip: { trigger: 'item', formatter: '{b}: {c}' },
            series: [{
                type: 'funnel',
                left: '8%',
                top: 16,
                bottom: 16,
                width: '84%',
                min: 0,
                max: Math.max.apply(null, items.map(function (x) { return x.value; }).concat([1])),
                sort: 'descending',
                gap: 4,
                label: { show: true, position: 'inside', formatter: '{b}: {c}' },
                data: items
            }]
        });
    }

    function renderGauge(container, data) {
        var el = ensureCanvas(container, 'dash-chart-canvas dash-chart-gauge');
        var value = Number(data?.value ?? data?.Value ?? 0);
        var max = Number(data?.max ?? data?.Max ?? 100);
        var unit = data?.unit ?? data?.Unit ?? '';
        var title = data?.title ?? data?.Title ?? '';

        initChart(el, {
            series: [{
                type: 'gauge',
                min: 0,
                max: max,
                progress: { show: true, width: 14 },
                axisLine: { lineStyle: { width: 14 } },
                axisTick: { show: false },
                splitLine: { length: 10, lineStyle: { width: 2, color: '#999' } },
                axisLabel: { distance: 18, fontSize: 10 },
                anchor: { show: true, size: 16, itemStyle: { borderWidth: 4 } },
                title: { show: !!title, offsetCenter: [0, '72%'], fontSize: 12 },
                detail: {
                    valueAnimation: true,
                    fontSize: 22,
                    offsetCenter: [0, '42%'],
                    formatter: function (v) { return v + unit; }
                },
                data: [{ value: value, name: title }]
            }]
        });
    }

    function render(container, chartType, data) {
        if (!container) return;
        dispose(container);

        var ct = parseInt(chartType, 10) || 1;
        switch (ct) {
            case 2:
                renderLine(container, data);
                break;
            case 3:
                renderBar(container, data);
                break;
            case 4:
                renderPie(container, data);
                break;
            case 5:
                renderTable(container, data);
                break;
            case 6:
                renderFunnel(container, data);
                break;
            case 7:
                renderGauge(container, data);
                break;
            default:
                container.innerHTML = '<div class="dash-card-loading">不支持的图表类型 ' + ct + '</div>';
        }
    }

    function resizeAll() {
        chartMap.forEach(function (inst) {
            try { inst.resize(); } catch (e) { /* ignore */ }
        });
    }

    global.DashCharts = {
        render: render,
        dispose: dispose,
        resizeAll: resizeAll
    };
})(window);
