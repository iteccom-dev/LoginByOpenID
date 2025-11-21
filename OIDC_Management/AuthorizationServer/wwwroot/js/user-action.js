



$(document).ready(function () {

    $(document).on("click", "#btn-create-user", function () {

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

    // Xóa
    $(document).off("click", "#btn-user-delete").on("click", "#btn-user-delete", function (e) {
        e.preventDefault();
        var id = $(this).data("id");
        showConfirmDialog({
            title: "Xóa tài khoản",
            message: "Bạn có chắc chắn muốn xóa tài khoản này?",
            confirmText: "Xóa",
            onConfirm: function () {
                deleteUser(id);
            },
            onCancel: function () {
                console.log("Đã hủy!");
            }
        });

    });
    const main_content = $("#content-main");
    $(document).off('click', '#btnCancelUser').on('click', '#btnCancelUser', function (e) {
        $.ajax({
            url: '/Home/UserList',
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                main_content.html(html);
            })
            .fail(function (xhr) {
                main_content.html('<div class="alert alert-danger">' + (xhr.responseText || 'Không tải được chi tiết') + '</div>');
            });
    });

    $(document).off("click", "#btn-user-edit").on("click", "#btn-user-edit", function (e) {
        e.preventDefault();

        let id = $(this).data("id");

        $.ajax({
            url: `/api/user/get/${id}`,
            method: "GET",
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                $("#content-main").html(html);

            })
            .fail(function (xhr) {
                $("#content-main").html(
                    '<div class="alert alert-danger">' +
                    (xhr.responseText || 'Không tải được chi tiết') +
                    '</div>'
                );
            });
    });






});


function deleteUser(id) {
    $.ajax({
        url: `api/user/delete/${id}`,
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
function getUserFormData() {
    return {
        userName: $("#userName").val(),
        userPassword: $("#userPassword").val(),
        userEmail: $("#userEmail").val(),
        userPhone: $("#userPhone").val(),

        userRoles: $("#userRoles").val(),
        userStatus: $("#userStatus").val(),
        userProvider: $("#userProvider").val(),
        userClient: $("#userClient").val(),

        user2FA: $("#user2FA").is(":checked"),

        createdBy: $("#createdBy").val(),
        updatedBy: $("#updatedBy").val(),
        createdAt: $("#createdAt").val(),
        updatedAt: $("#updatedAt").val()
    };
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



