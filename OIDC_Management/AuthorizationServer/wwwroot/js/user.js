
$(document).ready(function () {
    loadIndex();

//Event Handle
    //Hủy
   

    // Search theo input
  
    let searchTimer;
    $('.search').off("keyup").on("keyup", function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(() => {
            applyFilters(1);
        }, 800);
    });

    $('.form-select').off("change").on("change", function () {
     let   clientId = $(this).val();
        applyFilters(getCurrentPage(), clientId);
    });



});
function loadIndex() {
    applyFilters(1);
    getClientOption()
}

//FEARTURE

// Hàm load dữ liệu user
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
                            <td class="text-center">${index + 1 + (response.data.result.currentPage - 1) * response.data.result.pageSize}</td>
                            <td class="text-left">
                                <div class="d-flex flex-column gap-2">
                                    <strong class="text-primary text-wrap text-truncate-two-lines">${user.userName}</strong>
                                    <div class="text-muted"><span class="text-primary">${user.phoneNumber || ''}</span></div>
                                    <div class="text-body-tertiary">
                                        <span>Status: </span>
                                        <span class="badge ${user.status === 1 ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger'}">
                                            ${user.status === 1 ? 'Active' : 'Inactive'}
                                        </span>
                                    </div>
                                </div>
                            </td>
                            <td class="text-center">${user.email || ''}</td>
                            <td class="text-center">${user.phoneNumber}</td>
                          
                            <td class="text-center">${user.clientId || ''}</td>
                            <td class="text-center">
                                <div class="dropdown dropdown-action">
                                    <a href="#" class="btn btn-soft-primary btn-sm dropdown" data-bs-toggle="dropdown" aria-expanded="false">
                                        <i class="ri-more-2-fill"></i>
                                    </a>
                                    <ul class="dropdown-menu dropdown-menu-end">
                                        <li><a href="#" id="btn-user-view" class="dropdown-item view-item-btn text-primary" data-id="${user.id}">Xem chi tiết</a></li>
                                        <li><a href="#" id="btn-user-edit" class="dropdown-item edit-item-btn text-warning" data-id="${user.id}">Chỉnh sửa</a></li>
                                        <li><a href="#" id="btn-user-delete" class="dropdown-item remove-item-btn text-danger" data-id="${user.id}">Xóa bỏ</a></li>
                                    </ul>
                                </div>
                            </td>
                        </tr>
                        `;
                    tbody.append(tr);
                });
           
                renderPagination(
                    response.data.result.currentPage,
                    response.data.result.totalRecords,
                    response.data.result.pageSize,
                    filter.keyword,
                    response.data.result.items.clientId);
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

// Hàm render pagination
function renderPagination(
    current,
    total,
    pageSize,
    keyword,
    clientId) {
    const totalPages = Math.ceil(total / pageSize);
    if (totalPages <= 1) {
        $("#pagination").html("");
        return;
    }

    let html = `<div class="btn-group" role="group">`;

    const isFirst = current === 1;
    const isLast = current === totalPages;

    // 🔹 First & Prev
    html += `
        <label class="btn btn-outline-primary btn-paging ${isFirst ? "disabled" : ""}" data-page="1">« First</label>
        <label class="btn btn-outline-primary btn-paging ${isFirst ? "disabled" : ""}" data-page="${current - 1}">‹ Prev</label>
    `;

    const maxVisible = 5;
    let startPage = Math.max(1, current - Math.floor(maxVisible / 2));
    let endPage = Math.min(totalPages, startPage + maxVisible - 1);
    if (endPage - startPage < maxVisible - 1) {
        startPage = Math.max(1, endPage - maxVisible + 1);
    }

    // 🔹 Nếu có trang đầu ẩn
    if (startPage > 1) {
        html += `
            <label class="btn btn-outline-primary btn-paging" data-page="1">1</label>
            <span class="btn btn-light disabled">...</span>
        `;
    }

    // 🔹 Các trang giữa
    for (let i = startPage; i <= endPage; i++) {
        html += `
            <label class="btn btn-outline-primary btn-paging ${i === current ? 'active' : ''}"
                data-page="${i}"
               
                data-keyword="${keyword || ''}"
              
                data-status="${clientId || ''}">
                ${i}
            </label>`;
    }

    // 🔹 Nếu có trang cuối ẩn
    if (endPage < totalPages) {
        html += `
            <span class="btn btn-light disabled">...</span>
            <label class="btn btn-outline-primary btn-paging" data-page="${totalPages}">${totalPages}</label>
        `;
    }

    // 🔹 Next & Last
    html += `
        <label class="btn btn-outline-primary btn-paging ${isLast ? "disabled" : ""}" data-page="${current + 1}">Next ›</label>
        <label class="btn btn-outline-primary btn-paging ${isLast ? "disabled" : ""}" data-page="${totalPages}">Last »</label>
    `;

    html += `</div>`;
    $("#pagination").html(html);

    // ✅ Sự kiện click phân trang
    $("#pagination").off("click", ".btn-paging:not(.disabled)").on("click", ".btn-paging:not(.disabled)", function () {
        const page = $(this).data("page");
        const filterParams = getFilterParams(); // lấy toàn bộ filter hiện tại
        applyFilters(page, filterParams);
        window.scrollTo({ top: 0, behavior: "smooth" });
    });
}

//Hàm render ClientId option 
function getClientOption() {
    $.ajax({
        url: '/api/client/gets',
        method: 'GET',
        success: function (response) {
            if (response.success) {

                const select = $("#ClientIdOption");
                select.empty(); // clear cũ

                // option mặc định
                select.append(`<option value="" selected>Danh sách Client</option>`);

                // đổ dữ liệu vào select
                $.each(response.data, function (index, item) {
                    select.append(`
                        <option value="${item.id}" class = "form-select">
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
















//CALL BACK













//HEPLER
function applyFilters(page = 1) {
    const filters = getFilterData();
    filters.page = page;

    loadUsers(filters);
}
function getFilterData() {
    return {
        page: getCurrentPage(),
        pageSize: 2,
        KeySearch: $('.search').val() || '',
        ClientId: $('.form-select').val() || ''
    };
}
function getFilterParams() {
    return {
        KeySearch: $('.search').val() || '',
        ClientId: $('.form-select').val() || ''
    };
}

function getCurrentPage() {
    const currentLabel = $("label[data-page].active, label[data-page].checked");

    if (currentLabel.length === 0) return 1;

    return parseInt(currentLabel.data("page"));
}




