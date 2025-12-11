
$(document).ready(function () {
    loadSetTime();
    $(document).off("click", "#btnUpdateSettingTime").on("click", "#btnUpdateSettingTime", function () {

        let sTime = $("#sessionTime").val();
        let rtTime = $("#tokenTime").val();

        $.ajax({
            url: "/api/settime",
            type: "POST",
            data: {
                sTime: sTime,
                rtTime: rtTime
            },
            success: function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    loadSetTime();
                } else {
                    toastr.error(res.message);
                }

            },
            error: function (xhr) {
                toastr.error("Lỗi kết nối server");
            }
        });

    });

    $(document)
        .off("click", "#btnUpdateLogo")
        .on("click", "#btnUpdateLogo", function () {

            var mainFile = $("#mainLogo")[0].files[0];
            var smallFile = $("#smallLogo")[0].files[0];

            if (!mainFile && !smallFile) {
                toastr.info("Vui lòng chọn ít nhất một ảnh!");
                return;
            }

            var formData = new FormData();
            formData.append("mainLogo", mainFile);
            formData.append("smallLogo", smallFile);

            $.ajax({
                url: "/api/set-logo",
                type: "POST",
                data: formData,
                processData: false,
                contentType: false,
                success: function (res) {
                    if (res.success) {
                        toastr.success(res.message);
                        LoadLogo();
                    }
                },
                error: function () {
                    toastr.error("Upload thất bại!");
                }
            });
        });

});
    function loadSetTime() {
        $.ajax({
            url: "/api/get/settime",   // Đổi thành API của bạn
            type: "GET",
            success: function (res) {

                if (res.success && res.data) {

                    // Lặp từng item
                    res.data.forEach(item => {
                        if (item.section === "SetSessionTime") {
                            $("#sessionTime").val(item.value);
                        }

                        if (item.section === "SetTokenTime") {
                            $("#tokenTime").val(item.value);
                        }
                    });
                }
            },
            error: function () {
                console.log("Không thể kết nối server");
            }
        });

    };
function LoadLogo() {
    $.ajax({
        url: "/api/get/setlogo",
        type: "GET",
        dataType: "json",
        timeout: 10000,
        success: function (res) {

            if (!res || !res.success || !Array.isArray(res.data)) {
                console.warn("Dữ liệu logo không hợp lệ hoặc trống");
                return;
            }

            const mainLogo = res.data.find(item => item.section === "MainLogo");
            const smallLogo = res.data.find(item => item.section === "SubLogo" || item.section === "Favicon");

            // 🟦 Cập nhật logo NAV
            if (mainLogo?.value) {
                $("#navMainLogo").attr("src", mainLogo.value + "?t=" + new Date().getTime());
            }

            if (smallLogo?.value) {
                $("#navSmallLogo").attr("src", smallLogo.value + "?t=" + new Date().getTime());
            }
        },
        error: function (xhr, status, err) {
            console.error("Lỗi tải logo:", status, err);
        }
    });
}
