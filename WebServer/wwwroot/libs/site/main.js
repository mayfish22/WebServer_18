async function setLanguage(lang) {
    location.href = `/Account/SetLanguage?culture=${lang}&returnUrl=${encodeURIComponent(location.href)}`;
}

async function fetchData(method, url, data) {
    try {
        let settings = {
            method: method,
        };
        if (data !== null && data !== undefined && method.toLowerCase() === 'post') {
            settings.body = JSON.stringify(data);
        }
        const fetchResponse = await fetch(`${url}`, settings);
        const result = fetchResponse.json();
        return result;
    } catch (e) {
        return e;
    }
}

$.fn.CustomDataTable = async function (options) {
    const $dataTable = $(this);
    window.addEventListener("resize", function () {
        $dataTable.dataTable().api().responsive.rebuild();
        $dataTable.dataTable().api().responsive.recalc();
        $dataTable.dataTable().api().columns.adjust();
    });

    //https://developer.mozilla.org/zh-TW/docs/Web/JavaScript/Reference/Operators/Destructuring_assignment
    //解構賦值 (Destructuring assignment) 
    ({
        fetchColumns: fetchColumns,
        getDataUrl: getDataUrl,
        buttonRender: buttonRender,
        buttonRenderWidth: buttonRenderWidth,
        lang: lang,
    } = options);

    if (buttonRenderWidth === null || buttonRenderWidth === undefined)
        buttonRenderWidth = "150px";

    const sColmns = await fetchColumns;

    let defaultN = 0;
    let theader = ``;
    let columns = [];
    let columnDefs = [];

    // #序號
    if (true) {
        theader += `<th class="text-center">#</th>`;
        columns.push({
            "data": null,
        });
        columnDefs.push({
            "targets": defaultN,
            //"width": "50px",
            "searchable": false,
            "orderable": false,
            "render": function (data, type, row, meta) {
                const info = $dataTable.dataTable().api().page.info();
                return '<span style="text-align: center; display:block;">' + (info.page * info.length + meta.row + 1) + '</span>';
            }
        });
        defaultN++;
    }

    // 按鈕
    if (buttonRender !== null && buttonRender !== undefined) {
        theader += `<th class="text-center"></th>`;
        columns.push({
            "data": null,
        });
        columnDefs.push({
            "targets": defaultN,
            //"width": buttonRenderWidth,
            "searchable": false,
            "orderable": false,
            "render": function (data, type, row, meta) {
                return buttonRender(data, type, row, meta);
            }
        });
        defaultN++
    }

    //欄位
    for (let i = 0; i < sColmns.data.length; i++) {
        theader += `<th>${sColmns.data[i].displayName}</th>`;
        columns.push({
            "data": sColmns.data[i].name,
        });
        columnDefs.push({
            "targets": i + defaultN,
            "orderable": sColmns.data[i].sortingType === "Enabled",
            "render": function (data, type, row, meta) {
                //可在這控制欄位如何顯示
                return data;
            }
        });
    }

    //Header 顏色
    theader = `<thead class="table-light"><tr>${theader}</tr></thead>`;
    $dataTable.empty().append(theader);

    $dataTable.DataTable({
        "language": {
            "url": `/libs/DataTables/Languages/${lang}.json`,
        },
        "drawCallback": function (settings) {
            $dataTable.find('[data-toggle="tooltip"]').tooltip();
        },
        lengthMenu: [10, 20, 30, 40, 50],
        responsive: true,
        "processing": true,
        "serverSide": true,
        "ajax": {
            "url": getDataUrl,
            "type": "POST",
        },
        "ordering": true,
        "order": [],
        "columnDefs": columnDefs,
        "columns": columns
    });
}


async function customFetch(url, settings) {
    try {
        const fetchResponse = await fetch(`${url}`, settings);
        const result = fetchResponse.json();
        return result;
    } catch (e) {
        return e;
    }
}

$.fn.Select2User = async function (options, onchange) {
    const dataURL = '/Common/FetchUser';

    //option 初始值設定
    options = options || {};
    options.rowCount = options.rowCount || 30;
    options.language = options.language || 'zh-TW';
    options.placeholder = options.placeholder || ` `;
    options.allowClear = (options.allowClear == null || options.allowClear == undefined) ? true : options.allowClear;
    options.tags = options.tags || false;

    options.ajax = options.ajax || {};
    options.ajax.delay = options.ajax.delay || 250;
    options.ajax.processResults = options.ajax.processResults || function (data, params) {
        return {
            results: data.results,
            pagination: {
                "more": data.pagination
            }
        }
    };
    onchange = onchange || function (e) { };

    //可能會有多個 select
    const $objs = $(this);
    for (let ix = 0; ix < $objs.length; ix++) {
        let $obj = $objs.eq(ix);
        // 初始值可能會有多個
        let currentValue = $obj.attr('defaultvalue');
        if (currentValue === null || currentValue === undefined)
            currentValue = [];
        else
            currentValue = currentValue.split(',');

        //初始值
        const data = await customFetch(dataURL, {
                headers: {
                    'user-agent': navigator.userAgent,
                    'content-type': 'application/json',
                },
                body: JSON.stringify({ 'values': currentValue }),
                method: 'POST',
            });
        for (let i = 0; i < data.results.length; i++) {
            $obj.append(new Option(data.results[i].text, data.results[i].id, true, true));
        }

        options.ajax.transport = function (params, success, failure) {
            return customFetch(dataURL, {
                headers: {
                    'user-agent': navigator.userAgent,
                    'content-type': 'application/json',
                },
                body: JSON.stringify({
                    'page': params.data.page || 1, // 第幾頁
                    'rows': options.rowCount, // 每頁顯示幾行
                    'parameter': params.data.term,
                }),
                method: 'POST',
            }).catch(error => {
                failure(error);
            }).then(data => {
                success(data, params);
            });
        };
        //搜尋的結果
        options.templateResult = function (state) {
            if (!state.id || state.id === "") {
                return $(`<span>${options.placeholder}</span>`);
            }
            if ($obj.val() !== null && $obj.val() !== undefined)
                if ($obj.attr('multiple') !== undefined) {
                    if ($obj.val().map((x) => x.toUpperCase()).indexOf(state.id.toUpperCase()) >= 0) {
                        //排除已選的
                        return null;
                    }
                }
                else {
                    if ($obj.val().toUpperCase() === state.id.toUpperCase()) {
                        //排除已選的
                        return null;
                    }
                }
            let obj = JSON.parse(state.text);
            let title = `【${obj.account}】${obj.name}`.replace(/"/g, '&quot;');
            let template = `<span title="${title}">${title}</span>`;
            return $(template);
        };
        //選擇的資料
        options.templateSelection = function (state) {
            if (!state.id || state.id === "") {
                return $(`<span>${options.placeholder}</span>`);
            }
            let obj = JSON.parse(state.text);
            let title = `【${obj.account}】${obj.name}`.replace(/"/g, '&quot;');
            let template = `<span title="${title}">${title}</span>`;
            return $(template);
        };
        //設定change事件
        $obj.select2(options).on('change', onchange);
        //刪除初始設定值
        $obj.removeAttr('defaultvalue');
    }
    //回傳自身, 以便後續 chain
    return this;
}