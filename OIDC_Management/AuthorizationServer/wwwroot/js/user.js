$(document).ready(function () {
    loadIndex();

    // Search
    let searchTimer;
    $('.search').off("keyup").on("keyup", function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(() => {
            applyFilters(1);
        }, 800);
    });

    // Filter client trên list (select lọc)
    $('#ClientIdOption').off("change").on("change", function () {
        applyFilters(1);
    });

    $(document).off("click", "#user-add-link").on("click", "#user-add-link", function (e) {
        e.preventDefault();

        $.ajax({
            url: '/api/sync-users',
            type: 'GET',
            dataType: 'json',
            success: function (res) {
                // res.message nếu API trả về { success: true, message: "..." }
                Swal.fire("Thành công", res.message || "Đồng bộ thành công", "success");
            },
            error: function (xhr) {
                var errMsg = xhr.responseJSON?.message || xhr.statusText || "Có lỗi xảy ra";
                Swal.fire("Lỗi", errMsg, "error");
            }
        });
    });
    $(document).off("click", "#btnClearFillter").on("click", "#btnClearFillter", function (e) {
        e.preventDefault();
        
        $("#searchBox").val("");
        $("#ClientIdOption").val("");
        applyFilters(1);

       
    });


    // 🔥 NÚT LƯU TRONG FORM (Thêm + Sửa chung)
    $(document).off("click", "#btn-create-user").on("click", "#btn-create-user", function () {
        const data = collectUserForm();

        const hasId = data.Id && data.Id.trim() !== "";
        const url = hasId ? "/api/user/update" : "/api/user/create";

        $.ajax({
            url: url,
            type: "POST",
            data: JSON.stringify(data),
            contentType: "application/json",
            success: function (res) {
                Swal.fire("Thành công", res.message, "success");
                $("#userModal").modal("hide");
                applyFilters(1);
            },
            error: function (err) {
                const msg = err.responseJSON?.message || "Có lỗi xảy ra";
                Swal.fire("Lỗi", msg, "error");
            }
        });
    });
});

function loadIndex() {
    applyFilters(1);
    getClientOption();
}

// =================== LOAD LIST USER ===================
function loadUsers(filter) {
    $.ajax({
        url: '/api/user/gets',
        method: 'GET',
        data: filter,
        success: function (response) {
            const tbody = $('#content-user');
            tbody.empty();

            if (response.success) {
                const users = response.data.result.items;

                $.each(users, function (index, user) {
                    let tr = `
                        <tr>
                            <th class="text-center">
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" data-id="${user.id}">
                                </div>
                            </th>
                            <td class="text-center">
                                ${index + 1 + (response.data.result.currentPage - 1) * response.data.result.pageSize}
                            </td>
                            <td class="text-left">
                                <div class="d-flex flex-column gap-2">
                                    <strong class="text-primary text-wrap text-truncate-two-lines">
                                        ${user.userName}
                                    </strong>
                                    <div class="text-muted">
                                        <span class="text-primary">${user.phoneNumber || ''}</span>
                                    </div>
                                    <div class="text-body-tertiary">
                                        <span>Status: </span>
                                        <span class="badge ${user.status === 1 ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger'}">
                                            ${user.status === 1 ? 'Active' : 'Inactive'}
                                        </span>
                                    </div>
                                </div>
                            </td>
                            <td class="text-center">${user.email || ''}</td>
                            <td class="text-center">
                                <div class="dropdown dropdown-action">
                                    <a href="#" class="btn btn-soft-primary btn-sm dropdown" data-bs-toggle="dropdown">
                                        <i class="ri-more-2-fill"></i>
                                    </a>
                                    <ul class="dropdown-menu dropdown-menu-end">
                                       
                                        <li>
                                            <a href="#" id="btn-user-edit" class="dropdown-item edit-item-btn text-warning"
                                               data-id="${user.id}"><i class="ri-edit-fill fs-16"></i>Chỉnh sửa</a>
                                        </li>
                                        <li>
                                            <a href="#" id="btn-user-delete" class="dropdown-item remove-item-btn text-danger"
                                               data-id="${user.id}"><i class="ri-delete-bin-5-fill fs-16"></i>Xóa bỏ</a>
                                        </li>
                                         
                                    </ul>
                                </div>
                            </td>
                        </tr>`;
                    tbody.append(tr);
                });

                renderPagination(
                    response.data.result.currentPage,
                    response.data.result.totalRecords,
                    response.data.result.pageSize,
                    filter.KeySearch,
                    filter.ClientId
                );
            } else {
                tbody.append('<tr><td colspan="8" class="text-center">Không có dữ liệu</td></tr>');
                $('#pagination').empty();
            }
        },
        error: function (xhr) {
            alert('Lỗi server: ' + xhr.status);
        }
    });
}

// =================== CLIENT FILTER DROPDOWN Ở LIST ===================
function getClientOption() {
    $.ajax({
        url: '/api/client/gets',
        method: 'GET',
        success: function (response) {
            if (response.success) {
                const select = $("#ClientIdOption");
                select.empty();
                select.append(`<option value="" selected>Danh sách Client</option>`);

                $.each(response.data, function (index, item) {
                    select.append(`
                        <option value="${item.id}">
                            ${item.name}
                        </option>
                    `);
                });
            }
        },
        error: function (xhr) {
            alert('Lỗi server: ' + xhr.status);
        }
    });
}

// =================== FILTER & PAGING ===================
function applyFilters(page = 1) {
    const filters = getFilterData();
    filters.page = page;
    loadUsers(filters);
}

function getFilterData() {
    return {
        page: getCurrentPage(),
        pageSize: 10,
        KeySearch: $('.search').val() || '',
        ClientId: $('#ClientIdOption').val() || ''
    };
}

function getFilterParams() {
    return {
        KeySearch: $('.search').val() || '',
        ClientId: $('#ClientIdOption').val() || ''
    };
}

function getCurrentPage() {
    const currentLabel = $("label[data-page].active, label[data-page].checked");
    if (currentLabel.length === 0) return 1;
    return parseInt(currentLabel.data("page"));
}

function renderPagination(current, total, pageSize, keyword, clientId) {
    const totalPages = Math.ceil(total / pageSize);
    if (totalPages <= 1) {
        $("#pagination").html("");
        return;
    }

    let html = `<div class="btn-group" role="group">`;
    const isFirst = current === 1;
    const isLast = current === totalPages;

    html += `
        <label class="btn btn-outline-primary btn-paging ${isFirst ? "disabled" : ""}" data-page="1">« Đầu</label>
        <label class="btn btn-outline-primary btn-paging ${isFirst ? "disabled" : ""}" data-page="${current - 1}">‹ Trước</label>
    `;

    const maxVisible = 5;
    let startPage = Math.max(1, current - Math.floor(maxVisible / 2));
    let endPage = Math.min(totalPages, startPage + maxVisible - 1);
    if (endPage - startPage < maxVisible - 1) {
        startPage = Math.max(1, endPage - maxVisible + 1);
    }

    if (startPage > 1) {
        html += `
            <label class="btn btn-outline-primary btn-paging" data-page="1">1</label>
            <span class="btn btn-light disabled">...</span>
        `;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `
            <label class="btn btn-outline-primary btn-paging ${i === current ? 'active' : ''}"
                data-page="${i}"
                data-keyword="${keyword || ''}"
                data-status="${clientId || ''}">
                ${i}
            </label>`;
    }

    if (endPage < totalPages) {
        html += `
            <span class="btn btn-light disabled">...</span>
            <label class="btn btn-outline-primary btn-paging" data-page="${totalPages}">${totalPages}</label>
        `;
    }

    html += `
        <label class="btn btn-outline-primary btn-paging ${isLast ? "disabled" : ""}" data-page="${current + 1}">Sau ›</label>
        <label class="btn btn-outline-primary btn-paging ${isLast ? "disabled" : ""}" data-page="${totalPages}">Cuối »</label>
    `;

    html += `</div>`;
    $("#pagination").html(html);

    $("#pagination").off("click", ".btn-paging:not(.disabled)")
        .on("click", ".btn-paging:not(.disabled)", function () {
            const page = $(this).data("page");
            applyFilters(page, getFilterParams());
            window.scrollTo({ top: 0, behavior: "smooth" });
        });
}

// =================== LẤY DATA TỪ FORM (Thêm/Sửa) ===================
function collectUserForm() {
    const form = $("#userForm");
    let password = form.find("#userPassword").val();

    // Nếu vẫn là ******** thì không thay đổi mật khẩu
    if (password === "********") {
        password = "";
    }

    return {
        Id: form.find("#userId").val() || "",
        UserName: form.find("#userName").val(),
        UserPassword: password,
        UserEmail: form.find("#userEmail").val(),
        UserPhone: form.find("#userPhone").val(),
        UserStatus: parseInt(form.find("#userStatus").val(), 10),
        UserClient: form.find("#ClientId").val(),
        Role: parseInt($("#userRoles").val(), 10),

    };
}

