$(document).ready(function () {
    loadIndex();

    let searchTimer;
    $('.search').on("keyup", function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(() => applyFilters(1), 800);
    });

    // Filter Status 
    $('#clientStatusFilter').on('change', function () {
        applyFilters(1);
    });


   
});
//Thêm
$(document).on('click', '#btnAddClient', function (e) {
    e.preventDefault();

    $('.col-xl-12').has('.card-header .add-btn').hide();
    $('#addClientFormCard').slideDown();

    $('#clientForm')[0].reset();
    $('#clientId').val("");
    $('#clientSecret').val("");
});

//Hủy
$(document).on('click', '#btnCancelClient', function (e) {
    e.preventDefault();

    $('#addClientFormCard').slideUp();
    $('.col-xl-12').has('.card-header .add-btn').show();
});



$(document).on('click', '.edit-item-btn', function (e) {
    e.preventDefault();

    const clientId = $(this).data('id');

    if (!clientId) {
        toastr.error("Không tìm thấy ClientId!");
        return;
    }

    // Ẩn bảng danh sách
    $('.col-xl-12').has('.card-header .add-btn').hide();

    // Hiện form
    $('#addClientFormCard').slideDown();
});

function loadIndex() {
    applyFilters(1);
}

// ===============================
// GLOBAL


// LOAD CLIENT LIST
function loadClients(filter) {

    $.ajax({
        url: '/api/client',
        method: 'GET',
        data: filter,
        success: function (res) {
            console.log("Đã vào success loadClients");
            console.log(res);

            const tbody = $('#content');
            tbody.empty();
            console.log(tbody.length);

            tbody.empty();

            if (!res.success) {
                tbody.append(`<tr><td colspan="7" class="text-center">Không có dữ liệu</td></tr>`);
                $('#pagination').empty();
                return;
            }

            const data = res.data;
            const items = data.items;
            const totalRecords = data.totalRecords;
            const totalPages = Math.ceil(totalRecords / data.pageSize);

            $.each(items, function (index, item) {
                const row = `
                    <tr>
                        <th class="text-center">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" data-id="${item.clientId}">
                            </div>
                        </th>
                        <td class="text-center">${index + 1 + (data.page - 1) * data.pageSize}</td>
                        <td class="text-left">${item.clientId}</td>
                        <td class="text-left">${item.clientSecret || ''}</td>
                        <td class="text-left">${item.displayName || ''}</td>
                        <td class="text-center">
                            <span class="badge ${item.status === 1 ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger'}">
                                ${item.status === 1 ? 'Kích hoạt' : 'Tạm khóa'}
                            </span>
                        </td>

                        <td class="text-center">
                            <div class="dropdown dropdown-action">
                                <a href="#" class="btn btn-soft-primary btn-sm dropdown" data-bs-toggle="dropdown">                                
                                 <i class="ri-more-2-fill"></i>
                                </a>
                                <ul class="dropdown-menu dropdown-menu-end">
                                    <li>
                                        <a href="#" class="dropdown-item view-item-btn text-primary">
                                            <i class="ri-eye-fill fs-16"></i> Xem chi tiết
                                        </a>
                                    </li>
                                   <li>
                                        <a href="#" class="dropdown-item edit-item-btn text-warning" data-id="${item.clientId}">
                                            <i class="ri-edit-fill fs-16"></i> Chỉnh sửa
                                        </a>
                                    </li>

                                   <li>
                                        <a href="#"
                                           class="dropdown-item delete-item-btn text-danger" data-id="${item.clientId}">
                                            <i class="ri-delete-bin-5-fill fs-16"></i> Xóa bỏ
                                        </a>
                                    </li>

                                </ul>
                            </div>
                        </td>
                    </tr>`;

                tbody.append(row);
            });

            renderPagination(data.page, totalRecords, data.pageSize);
        },
        error: function (xhr) {
            alert("Lỗi server: " + xhr.status);
        }
    });
}



$("#content-main").on("click", ".delete-item-btn", function (e) {
    e.preventDefault();
    const id = $(this).data("id");
    console.log("Click delete, id=", id);

    if (!id) {
        toastr.error("Không có ID để xóa!");
        return;
    }

    if (confirm("Bạn có chắc chắn muốn xóa client này không?")) {
        $.ajax({
            url: "/api/client/delete",
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({ id: id }),
            success: function (response) {
                console.log("Response:", response);
                if (response.success) {
                    toastr.success(response.message);
                    $(e.currentTarget).parents("tr").remove();
                } else {
                    toastr.warning(response.message);
                }
            },
            error: function (xhr) {
                console.log("Error:", xhr);
                toastr.error("Không thể kết nối tới server!");
            }
        });
    }
});




// ===============================
// PAGINATION
// ===============================
function renderPagination(current, total, pageSize) {
    const totalPages = Math.ceil(total / pageSize);

    if (totalPages <= 1) {
        $("#pagination").empty();
        return;
    }

    let html = `<div class="btn-group">`;

    const isFirst = current === 1;
    const isLast = current === totalPages;

    html += `
        <label class="btn btn-outline-primary btn-paging ${isFirst ? "disabled" : ""}" data-page="1">« First</label>
        <label class="btn btn-outline-primary btn-paging ${isFirst ? "disabled" : ""}" data-page="${current - 1}">‹ Prev</label>
    `;

    let maxVisible = 5;
    let start = Math.max(1, current - Math.floor(maxVisible / 2));
    let end = Math.min(totalPages, start + maxVisible - 1);

    if (start > 1) {
        html += `<label class="btn btn-outline-primary btn-paging" data-page="1">1</label>
                 <span class="btn btn-light disabled">...</span>`;
    }

    for (let i = start; i <= end; i++) {
        html += `<label class="btn btn-outline-primary btn-paging ${i === current ? 'active' : ''}" data-page="${i}">${i}</label>`;
    }

    if (end < totalPages) {
        html += `<span class="btn btn-light disabled">...</span>
                 <label class="btn btn-outline-primary btn-paging" data-page="${totalPages}">${totalPages}</label>`;
    }

    html += `
        <label class="btn btn-outline-primary btn-paging ${isLast ? "disabled" : ""}" data-page="${current + 1}">Next ›</label>
        <label class="btn btn-outline-primary btn-paging ${isLast ? "disabled" : ""}" data-page="${totalPages}">Last »</label>
    `;

    html += `</div>`;

    $("#pagination").html(html);

    $("#pagination").off("click", ".btn-paging:not(.disabled)").on("click", ".btn-paging:not(.disabled)", function () {
        applyFilters($(this).data("page"));
        window.scrollTo({ top: 0, behavior: "smooth" });
    });
}



//sửa
$(document).on('click', '.edit-item-btn', function (e) {
    e.preventDefault();

    const clientId = $(this).data('id');

    if (!clientId) {
        toastr.error("Không tìm thấy ClientId!");
        return;
    }

    $('.col-xl-12').has('.card-header .add-btn').hide();
    $('#addClientFormCard').slideDown();

    $.ajax({
        url: `/api/client/${clientId}/edit-json`,
        method: "GET",
        success: function (res) {
            if (!res.success) {
                toastr.error("Không tìm thấy dữ liệu client!");
                return;
            }

            const c = res.data;

            // Gán dữ liệu vào form
            $('#clientId').val(c.clientId);
            $('#clientSecret').val(c.clientSecret);
            $('#displayName').val(c.displayName);
            $('#redirectUris').val(c.redirectUris);
            $('#callbackPath').val(c.callbackPath);
            $('#accessDeniedPath').val(c.accessDeniedPath);
            $('#scope').val(c.scope);
            $('#grantType').val(c.grantType);
            $('#authority').val(c.authority);
            $('#keyWord').val(c.keyWord);
            $('#clientStatus').val(c.status);
        },
        error: function () {
            toastr.error("Không thể tải dữ liệu client!");
        }
    });
});



// SUBMIT FORM - CREATE OR UPDATE CLIENT
$(document).on('submit', '#clientForm', function (e) {
    e.preventDefault();

    const isEdit = $('#clientId').val() !== "";
    const apiUrl = isEdit ? "/api/client/update" : "/api/client/create";

    const requestData = {
        clientId: $('#clientId').val(),
        displayName: $('#displayName').val(),
        redirectUris: $('#redirectUris').val(),
        scope: $('#scope').val(),
        grantType: $('#grantType').val(),
        authority: $('#authority').val(),
        status: parseInt($('#clientStatus').val()),

        callbackPath: $('#callbackPath').val(),
        accessDeniedPath: $('#accessDeniedPath').val(),
        keyword: $('#keyWord').val()
    };

    $.ajax({
        url: apiUrl,
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(requestData),
        success: function (res) {
            if (res.success) {
                toastr.success(res.message);

                $('#addClientFormCard').slideUp();
                $('.col-xl-12').has('.card-header .add-btn').show();

                applyFilters(1);
                $('#clientForm')[0].reset();
            } else {
                toastr.warning(res.message);
            }
        },
        error: function (xhr) {
            toastr.error("Lỗi server: " + xhr.status);
        }
    });
});




// FILTER + APPLY
function applyFilters(page = 1) {
    const filters = {
        page: page,
        pageSize: 5,
        keySearch: $('.search').val() || '',
        status: $('#clientStatusFilter').val() === "" ? null : $('#clientStatusFilter').val()
    };

    loadClients(filters);
}


