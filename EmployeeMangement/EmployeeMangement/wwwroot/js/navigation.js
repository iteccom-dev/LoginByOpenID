$(function () {

    //Các nút chuyển trang
    const main_content = $("#content-main");
    $("#employee-list-link").on("click", function () {

        $.ajax({
            url: '/Employee/EmployeeList',
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
    $("#department-list-link").on("click", function () {

        $.ajax({
            url: '/Department/DepartmentList',
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
    $("#jobposition-list-link").on("click", function () {

        $.ajax({
            url: '/JobPosition/JobpositionList',
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


    $("#nav-change-link").on("click", function () {
     
        $.ajax({
            url: '/Employee/ChangePassword',
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
    $(document).on("click", ".password-toggle", function () {
        const input = $(this).siblings("input");
        const icon = $(this).find("i");

        if (input.attr("type") === "password") {
            input.attr("type", "text");
            icon.removeClass("ri-eye-fill").addClass("ri-eye-off-fill");
        } else {
            input.attr("type", "password");
            icon.removeClass("ri-eye-off-fill").addClass("ri-eye-fill");
        }
    });

    // Check match password real-time
    $(document).on("click", "#btnChangePassword", function (e) {
        e.preventDefault(); // Ngăn load lại trang

        const oldPass = $("#oldPassword").val().trim();
        const newPass = $("#newPassword").val().trim();
        const confirmPass = $("#confirmPassword").val().trim();

        // Kiểm tra mật khẩu cũ
        if (!oldPass) {
            toastr.warning("Vui lòng nhập mật khẩu cũ!");
            $("#oldPassword").focus();
            return;
        }

        // Kiểm tra xác nhận mật khẩu
        if (newPass !== confirmPass) {
            toastr.error("Mật khẩu xác nhận không trùng khớp!");
            $("#confirmPassword").focus();
            return; 
        }

        // Gửi AJAX đổi mật khẩu
        $.ajax({
            url: '/api/employee/change',
            type: 'POST',
            data: {
                oldPassword: oldPass,
                newPassword: newPass
            }
        })
            .done(function (res) {
                if (res.success) {
                    toastr.success("Đổi mật khẩu thành công!");
                    $("#oldPassword, #newPassword, #confirmPassword").val("");
                } else {
                    toastr.error(res.message || "Không đổi được mật khẩu!");
                }
                if (res.success = false) {
                    toastr.error("Mật khẩu không chính xác");
                }
            })
            .fail(function () {
                toastr.error("Mật khẩu không chính xác vui lòng thử lại");
            });
    });


});