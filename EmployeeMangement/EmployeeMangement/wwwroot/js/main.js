
$(function () {
    loadIndex();
   
    // sự kiện khi thay đổi bộ lọc
    $("#positionFilter, #departmentFilter, #jobPositionFilter,#createDateFrom, #createDateTo, #statusFilter, #searchBox,#quantityView")
        .on("change keyup", function () {
            applyFilters(1);
        });

    // nút xóa bộ lọc
    $("#clearFilter").on("click", function () {
        $("#keyword").val("");
        $("#departmentFilter").val("");
        $("#positionFilter").val("");
        $("#statusFilter").val("");
        $("#jobPositionFilter").val("");
        $("#createDateFrom").val("");
        $("#createDateTo").val("");

        applyFilters(1);
    });

    // lọc theo từ khóa
    let searchTimer;
    $("#keyword").on("keyup", function () {
        clearTimeout(searchTimer);

        searchTimer = setTimeout(() => {
            applyFilters(1);
        }, 800);
    });

    // Xem
    $(document).on("click", "#view-employee-btn", function (e) {
        e.preventDefault();
    
        const main_content = $("#modalView");
        main_content.empty();
        var id = $(this).data("id");
        var mode = "view";
        $.ajax({
            url: `/api/employee/${id}/${mode}`,
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                main_content.html(html);
                openViewModal();
            })
            .fail(function (xhr) {
                toastr.warning("Không có dữ liệu");

            });
       
    });
    //đóng modal 
    $(document).on("click", "#close-viewmodal-btn", function () {
        $("#modalView").empty();
    });
  
    $(document).on("click", "#close-edit-btn", function () {
        $("#modalEdit").empty();
    });
    // Sửa
    $(document).on("click", "#edit-employee-btn", function (e) {
        e.preventDefault();

        const main_content = $("#modalEdit");
       
        var id = $(this).data("id");
        var mode = "edit";
        $.ajax({
            url: `/api/employee/${id}/${mode}`,
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                main_content.html(html);
              
                openViewModal();
            })
            .fail(function (xhr) {
                toastr.warning("Không có dữ liệu");

            });  
    });





    // resetpassword
    $(document).on("click", "#resetpassword-btn", function (e) {
        e.preventDefault();
        var email = $("#detailEmail").val();
        var id = $("#idEmployee").val();
        showConfirmDialog({
            title: "Đặt lại mật khẩu",
            message: "Bạn có chắc chắn muốn đặt lại mật khẩu cho nhân viên này?",
            confirmText: "Đặt lại",

            onConfirm: function () {
                toastr.info("Đang thực hiên yêu cầu vui lòng chờ trong giây lát");
                $.ajax({
                    url: `/api/employee/reset/${email}/${id}`,
                    method: 'POST',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                })
                    .done(function (res) {
                        if (res.success)
                        toastr.success("Đặt lại mật khẩu thành công");
                        if (!res.success)
                            toastr.warning("Yêu cầu không hợp lệ kiểm tra lại email và quyền hạn của bạn");
                    })
                    .fail(function () {
                      
                        toastr.success("Yêu cầu không hợp lệ kiểm tra lại email và quyền hạn của bạn");

                    });  
            },
            onCancel: function () {
                console.log("Đã hủy!");
            }
        });
       
       
    });


    // Xóa
    $(document).on("click", "#delete-employee-btn", function (e) {
        e.preventDefault();
        var id = $(this).data("id");
        showConfirmDialog({
            title: "Xóa nhân viên",
            message: "Bạn có chắc chắn muốn xóa nhân viên này?",
            confirmText: "Xóa",
            onConfirm: function () {
                deleteEmployee(id);
            },
            onCancel: function () {
                console.log("Đã hủy!");
            }
        });
       
    });

    //Thêm
    $("#btnAdd").on("click", function (e) {
        e.preventDefault();
        const main_content = $("#modalView");
        $.ajax({
            url: '/Employee/AddEmployee',
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                main_content.html(html);
                
                openViewModal();
            })
            .fail(function (xhr) {
                toastr.warning("Không có dữ liệu");

            });

       
    });

    //Lưu
    $(document).on("click", "#saveEditBtn", function () {

        const items = getEmployeeEditData();

        // Nếu Id tồn tại và > 0 → Update, ngược lại → Create
        const isUpdate = items.Id && items.Id > 0;

        const apiUrl = isUpdate ? `/api/employee/update` : `/api/employee/create`;
        const httpMethod = isUpdate ? "PUT" : "POST";

        $.ajax({
            url: apiUrl,
            method: httpMethod,
            data: JSON.stringify(items),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        })
            .done(function (res, textStatus, xhr) {

                const statusCode = xhr.status;
                const isSuccess = res.success;
                const message = res.message || "Thực thi thành công";

                if (statusCode === 200 && isSuccess) {
                    toastr.success(message);

                    $('#employeeDetailModal').modal('hide');
                    $("#employeeDetailModal").empty();

                    let page = getCurrentPage();
                    applyFilters(page);
                }
                else if (statusCode === 400) {
                    toastr.warning(res.message || "Dữ liệu không hợp lệ!");
                }
                else if (statusCode === 404) {
                    toastr.warning("Không tìm thấy dữ liệu!");
                }
                else if (statusCode === 401) {
                    toastr.error("Bạn chưa đăng nhập!");
                 
                }
                else {
                    toastr.error(res.message || "Có lỗi xảy ra!");
                }
            })
            .fail(function (xhr) {
                const statusCode = xhr.status;

                if (statusCode === 500) {
                    toastr.error("Lỗi server!");
                } else if (statusCode === 0) {
                    toastr.error("Không thể kết nối đến server!");
                } else {
                    toastr.info("Vui lòng điền đủ thông tin");
                }
            });

    });












 





});
function loadIndex() {
    getDepartments("#departmentFilter");
    getJobPosition("#jobPositionFilter");
    applyFilters(1);
}

// Lấy danh sách tên danh mục trong bộ lọc
function getDepartments(positionFill) {
    $.ajax({
        url: 'api/departments/name',
        method: 'GET'

    })
        .done(function (res) {
            if (res.success) {
                rederDepartmentOptions(res, positionFill);
            } else {
                alert("Không có dữ liệu!");
            }
        })
        .fail(function () {
            alert("lỗi");
        });
}

function rederDepartmentOptions(res, positionFill) {
    var $select = $(`${positionFill}`);
    $select.empty();

    $select.append('<option value="">Tất cả</option>');

    res.data.forEach(function (item) {
        $select.append(`<option value="${item.id}">${item.name}</option>`);
    });
}

// Lấy danh sách tên địa chỉ công tác trong bộ lọc
function getJobPosition(positionFill) {
    $.ajax({
        url: 'api/jobposition/name',
        method: 'GET'

    })
        .done(function (res) {
            if (res.success) {
                rederJobPositionOptions(res, positionFill);
            } else {
                toastr.warning("Không có dữ liệu!");

            }
        })
        .fail(function () {
            toastr.error("Lỗi kết nối server");
        });
}

function rederJobPositionOptions(res, positionFill) {
    var $select = $(`${positionFill}`);
    $select.empty();

    $select.append('<option value="">Tất cả</option>');
    
    res.data.forEach(function (item) {
        $select.append(`<option value="${item.id}">${item.name}</option>`);
    });
}

// Lấy danh sách danh viên

function loadEmployees(filters) {
    $.ajax({
        url: 'api/employees',
        type: 'GET',
        data: filters,
        success: function (res) {
            console.log("API Response:", res);

            if (res.success && res.success.items && res.success.items.length > 0) {

                $("#noData").addClass("d-none");

                const result = res.success;

                renderEmployeeList(result.items);
                console.log("Đã đổ dữ liệu:", result.items);

                renderPagination(
                    result.currentPage,
                    result.totalRecords,
                    result.pageSize,
                    filters.departmentId,
                    filters.keyword,
                    filters.position,
                    filters.status
                );

            } else {
                $("#employeeList").empty();
                $("#noData").removeClass("d-none");
            }

        },
        error: function (xhr) {
            toastr.error("Server error: " + xhr.status);
        }
    });
}


function renderEmployeeList(items) {
    var $list = $("#employeeList");
    $list.empty();

    items.forEach(function (item) {

        const avatar = `https://ui-avatars.com/api/?name=${encodeURIComponent(item.fullname)}&background=4361ee&color=fff&size=60&bold=true`;
        const statusBadge = item.status === 1
            ? '<span class="badge bg-success status-badge">Kích hoạt</span>'
            : '<span class="badge bg-danger status-badge">Tạm khóa</span>';

        const createDate = item.createDate
            ? new Date(item.createDate).toLocaleString("vi-VN", {
                day: "2-digit",
                month: "2-digit",
                year: "numeric",
                hour: "2-digit",
                minute: "2-digit",
                second: "2-digit",
                hour12: false
            })
            : "—";


        const keyword = item.keyword || "—";

        var html = `
        <div class="list-group-item employee-item">
            <div class="row align-items-center">
                
                <div class="col-auto">
                    <input type="checkbox" class="form-check-input">
                </div>

                <div class="col-1">
                    <span class="fw-bold text-primary">${item.id}</span>
                </div>

                <div class="col-2">
                    <img src="${avatar}" class="avatar" alt="Avatar">
                </div>

                <div class="col-2">
                    <div class="employee-name">${item.fullname}</div>
                    <div class="info-text">
                        <i class="ri-mail-line text-primary"></i> ${item.email}
                    </div>

                    <div class="d-flex align-items-center gap-2 mt-2 flex-wrap">
                        <i class="ri-phone-line text-success"></i> ${item.phone}
                        ${statusBadge}
                    </div>

                    <div class="d-flex align-items-center mt-2 flex-wrap">
                        <span class="badge bg-light text-dark border">Ngày tạo:</span>
                        <span class="text-muted fw-medium">${createDate}</span>

                        <span class="badge bg-light text-dark border mt-1">Keyword:</span>
                        <span class="text-muted fw-medium">${keyword}</span>
                    </div>
                </div>

                <div class="col-1 text-center fw-medium text-dark">${item.position}</div>
                <div class="col-2 text-center fw-medium text-dark">${item.departmentName}</div>
                <div class="col-2 text-center fw-medium text-dark">${item.jobPositionName}</div>

                <div class="col-1 text-center">
                    <div class="dropup dropdown-action">
                        <a href="#" class="btn btn-soft-primary btn-sm dropdown" data-bs-toggle="dropdown">
                            <i class="ri-more-2-fill"></i>
                        </a>
                       <ul class="dropdown-menu dropdown-menu-end">
    <li>
        <a href="#" class="dropdown-item text-primary view-employee" id="view-employee-btn" data-id="${item.id}">
            <i class="ri-eye-fill fs-16"></i> Xem
        </a>
    </li>
    <li>
        <a href="#" class="dropdown-item text-warning edit-employee" id="edit-employee-btn" data-id="${item.id}">
            <i class="ri-edit-fill fs-16"></i> Sửa
        </a>
    </li>
    <li>
        <a href="#" class="dropdown-item text-danger delete-employee" id="delete-employee-btn" data-id="${item.id}">
            <i class="ri-delete-bin-5-fill fs-16"></i> Xóa
        </a>
    </li>
</ul>
                    </div>
                </div>

            </div>
        </div>`;

        $list.append(html);
    });
}

// tạo phân trang
function renderPagination(
    current,
    total,
    pageSize,
    departmentId,
    keyword,
    position,
    status
) {
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
                data-department="${departmentId || ''}"
                data-keyword="${keyword || ''}"
                data-position="${position || ''}"
                data-status="${status || ''}">
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

//Hàm lấy thông tin chi tiết nhân viên
function getEmployee() {
    $.ajax({
        url: `api/employee/${id}`,
        method: 'GET'

    })
        .done(function (res) {
            if (res.success) {
               
               
            } else {
                toastr.warning("Không có dữ liệu")
            }
        })
        .fail(function () {
            toastr.error("không kết nối được server");
        });
}

//Hàm xóa thông tin nhân viên
function deleteEmployee(id) {

    $.ajax({
        url: `api/employee/delete/${id}`,
        method: 'POST'

    })
        .done(function (res) {
            if (res.success) {
                toastr.success("Xóa thông tin thành công")
                let page = getCurrentPage();
                applyFilters(page);
            } else {
                toastr.warning("Xóa thông tin không thành công")
            }
        })
        .fail(function () {
            toastr.error("không kết nối được server");
        });
}


//Hàm trợ giúp lấy dữ liệu 

function applyFilters(page = 1) {
    const filters = getFilterData();
    filters.page = page;

    loadEmployees(filters);
}
function getFilterData() {
    return {
        page: getCurrentPage(),
        pageSize: $("#quantityView").val() || "",

        keySearch: $("#keyword").val()?.trim() || "",
        departmentId: $("#departmentFilter").val() || "",
        jobpositionId: $("#jobPositionFilter").val() || "",
        position: $("#positionFilter").val() || "",
        status: $("#statusFilter").val() || "",
        createDateFrom: $("#createDateFrom").val() || "",
        createDateTo: $("#createDateTo").val() || ""
    };
}
function getFilterParams() {
    return {
        keyword: $("#keyword").val() || "",
        status: $("#statusFilter").val() || "",
        departmentCode: $("#departmentFilter").val() || "",
        jobPositionCode: $("#jobFilter").val() || "",
        position: $("#positionFilter").val() || "",
    };
}
function getCurrentPage() {
    const currentLabel = $("label[data-page].active, label[data-page].checked");

    if (currentLabel.length === 0) return 1;

    return parseInt(currentLabel.data("page"));
}
function getEmployeeEditData() {
    return {
        Id: $("#idEmployee").val().trim(),
       // AvatarUrl: $("#txtAvatarUrl").val().trim(),
        Fullname: $("#detailName").val().trim(),
        Status: $("#detailStatus").val(),
        Email: $("#detailEmail").val().trim(),
        Phone: $("#detailPhone").val().trim(),
        Position: $("#detailPosition").val(),              // Chức vụ
        DepartmentId: $("#detailDepartment").val(),        // Phòng ban
        JobPositionId: $("#detailJobPosition").val(),      // Vị trí công tác
        Keyword: $("#detailKeywords").val().trim()
    };
}

  function openViewModal() {
      const modalElement = $('#employeeDetailModal');
    if (!modalElement) {
        console.error('Không tìm thấy modal #employeeDetailModal');
        return;
    }

    const modal = new bootstrap.Modal(modalElement, {
        backdrop: 'static', // Tùy chọn: không cho đóng khi click ngoài
        keyboard: true
    });

    modal.show();
}
function showConfirmDialog({
    title = "Xác nhận",
    message = "Bạn có chắc chắn muốn thực hiện hành động này không?",
    confirmText = "Đồng ý",
    cancelText = "Hủy",
    onConfirm = () => { },
    onCancel = () => { }
}) {
    // Tạo DOM popup
    const modal = $(`
        <div class="custom-confirm-modal">
            <div class="modal-content" >
                <h5 class="modal-title">${title}</h5>
                <p>${message}</p>
                <div class="modal-actions">
                    <button class="btn btn-secondary cancel-btn">${cancelText}</button>
                    <button class="btn btn-danger confirm-btn">${confirmText}</button>
                </div>
            </div>
        </div>
    `);

    // Style cơ bản
    modal.css({
        position: "fixed",
        top: 0, left: 0,
        width: "100%", height: "100%",
        background: "rgba(0,0,0,0.5)",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        zIndex: 9999
    });

    modal.find(".modal-content").css({
        background: "#fff",
        padding: "20px",
        borderRadius: "8px",
        minWidth: "300px",
        textAlign: "center",
        width: "65%"
    });

    // Gắn modal vào DOM
    $("body").append(modal);

    // Event button
    modal.find(".confirm-btn").on("click", function () {
        modal.remove();
        onConfirm();
    });

    modal.find(".cancel-btn").on("click", function () {
        modal.remove();
        onCancel();
    });
}
