$(function () {

    $("#email-forget-btn").on("click", function () {
        var email = $("#email-forget-val").val();

        if (!email) {
            Swal.fire({
                icon: 'warning',
                title: 'Thiếu thông tin!',
                text: 'Vui lòng nhập email của bạn.',
                timer: 1800,
                showConfirmButton: false
            });
            return;
        }
        Swal.fire({
            icon: 'info',
            title: 'Vui lòng chờ trong giây lát!',
            text: 'Đang tiến hành xử lý yêu cầu của bạn.',
            timer: 6000,
            showConfirmButton: false
        });
        $.ajax({
            url: `/api/account/reset/${email}`,
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (res) {
                if (res.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Gửi thành công!',
                        text: res.message || 'Mật khẩu mới đã gửi vào email của bạn.',
                        showConfirmButton: true,
                        confirmButtonText: 'OK'
                    }).then(() => {
                        // Reset input và điều hướng nếu cần
                        $("#email-forget-val").val("");
                    });
                } else {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Không thành công!',
                        text: res.message || 'Không tìm thấy email phù hợp.'
                    });
                }
            })
            .fail(function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi hệ thống!',
                    text: 'Vui lòng thử lại sau.'
                });
            });
    });







});