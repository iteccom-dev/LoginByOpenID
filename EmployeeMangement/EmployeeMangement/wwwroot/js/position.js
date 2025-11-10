$(function () {
  


    loadJobPosition();
    function loadJobPosition(filters) {
        $.ajax({
            url: '/api/jobposition/name',
            type: 'GET',
            data: filters,
            success: function (res) {
                if (res.success && res.data?.length > 0) {
                    $("#noData").addClass("d-none");
                    renderJobPositionList(res.data);
                } else {
                    $("#jobPositionList").empty();
                    $("#noData").removeClass("d-none");
                }
            },
            error: function (xhr) {
                toastr.error("Server error: " + xhr.status);
            }
        });
    }

     
    function renderJobPositionList(items) {
        const $list = $("#jobPositionList");
        $list.empty();

        items.forEach((item, index) => {
            const statusBadge = item.status === 1
                ? '<span class="badge bg-success">Kích hoạt</span>'
                : '<span class="badge bg-warning text-dark">Tạm khóa</span>';

            $list.append(`
                <tr>
                    <td class="text-center"><input type="checkbox" class="form-check-input"></td>
                    <td><span class="fw-bold text-primary">${index + 1}</span></td>
                    <td><span class="fw-medium">${item.code || "VP00" + item.id}</span></td>
                    <td><span class="fw-medium text-truncate d-inline-block" style="max-width:140px;">${item.name || ""}</span></td>
                    <td><span class="fw-medium text-truncate d-inline-block" style="max-width:120px;">${item.keyword || "-"}</span></td>
                    <td><span class="fw-medium text-truncate d-inline-block" style="max-width:150px;">${item.address || ""}</span></td>
                    <td class="text-center">${statusBadge}</td>
                    <td class="text-center">
                        <div class="dropup dropdown-action">
                            <a href="#" class="btn btn-soft-primary btn-sm dropdown" data-bs-toggle="dropdown" aria-expanded="false">
                                <i class="ri-more-2-fill"></i>
                            </a>
                            <ul class="dropdown-menu dropdown-menu-end shadow-sm">
                                <li>
                                    <a href="#" class="dropdown-item view-item-btnjob text-primary" data-id="${item.id}">
                                        <i class="ri-eye-fill fs-16 me-1"></i> Xem chi tiết
                                    </a>
                                </li>
                                <li>
                                    <a href="#" class="dropdown-item edit-item-btnjob text-warning" data-id="${item.id}">
                                        <i class="ri-edit-fill fs-16 me-1"></i> Chỉnh sửa
                                    </a>
                                </li>
                                <li>
                                    <a href="#" class="dropdown-item delete-item-btnjob text-danger" data-id="${item.id}">
                                        <i class="ri-delete-bin-5-fill fs-16 me-1"></i> Xóa bỏ
                                    </a>
                                </li>
                            </ul>
                        </div>
                    </td>
                </tr>
            `);
        });
    }

     $("#content-main").on("click", ".view-item-btnjob", async function (e) {
        e.preventDefault();
        const id = $(this).data("id");

        if (!id) return toastr.error("Không xác định được ID vị trí công tác!");

        try {
            const res = await $.getJSON(`/api/jobposition/view/${id}`);
            if (res.success && res.data) {
                const d = res.data;
                $("#detailJobPositionCode").val(d.code || "—");
                $("#detailJobPositionName").val(d.name || "—");
                $("#detailKeyword").val(d.keyword || "—");
                $("#detailAddress").val(d.address || "—");
                $("#detailStatusText").val(d.status === 1 ? "Kích hoạt" : "Tạm khóa");
 
                $("#detailCreatedBy").val(d.createBy ? d.createBy.fullName : "—");
                $("#detailCreatedDate").val(d.createDate ? new Date(d.createDate).toLocaleString() : "—");
                 $("#detailUpdatedBy").val(d.updateBy ? d.updateBy.fullName : "—");

                $("#detailUpdatedDate").val(d.updatedDate ? new Date(d.updatedDate).toLocaleString() : "—");
                new bootstrap.Modal(document.getElementById("jobPositionDetailModal")).show();
            } else {
                toastr.warning(res.message || "Không tìm thấy vị trí công tác!");
            }
        } catch (err) {
             toastr.error("Không thể tải chi tiết vị trí công tác!");
        }
    });

    $('#jobPositionDetailModal').on('hidden.bs.modal', function () {
        $('body').removeClass('modal-open');
        $('.modal-backdrop').remove();
    });

     async function openJobPositionModal(id = 0) {
        const modal = new bootstrap.Modal(document.getElementById("jobPositionCreateUpdate"));
        $("#jobPositionForm")[0].reset();
        $("#jobPositionId").val(id || 0);

        if (id === 0) {
            $("#createdBy").val(window.currentUserName || "Không xác định");
            $("#createdDate").val(new Date().toLocaleString());
            $("#updatedBy").val("");
            $("#updatedDate").val("");
            modal.show();
            return;
        }

        const res = await $.getJSON(`/api/jobposition/view/${id}`);
        if (res.success && res.data) {
            const d = res.data;
            $("#jobPositionId").val(d.id);
            $("#jobCode").val(d.code);
            $("#jobName").val(d.name);
            $("#jobKeyword").val(d.keyword || "");
            $("#jobAddress").val(d.address || "");
            $("#jobStatus").val(d.status);
            $("#createdBy").val(d.createBy?.fullName || "—");
            $("#createdDate").val(d.createDate ? new Date(d.createDate).toLocaleString() : "");
            $("#updatedBy").val(window.currentUserName || "Không xác định");
            $("#updatedDate").val(new Date().toLocaleString());
            modal.show();
        } else {
            toastr.warning(res.message || "Không tìm thấy vị trí công tác!");
        }
    }

     $(document).off("click", "#btnSaveJobPosition").on("click", "#btnSaveJobPosition", async function () {
         const id = parseInt($("#jobPositionId").val()) || 0;
        const model = {
            Id: id,
            Code: $("#jobCode").val().trim(),
            Name: $("#jobName").val().trim(),
            Keyword: $("#jobKeyword").val().trim(),
            Address: $("#jobAddress").val().trim(),
            Status: parseInt($("#jobStatus").val()),
            CreateBy: id === 0 ? parseInt(window.currentUserId) : null,
            UpdatedBy: id > 0 ? parseInt(window.currentUserId) : null
        };

        if (!model.Code || !model.Name) {
            toastr.warning("Vui lòng nhập đầy đủ Mã và Tên vị trí!");
            return;
        }

        try {
            const res = await $.ajax({
                url: "/api/jobposition/save",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(model)
            });

            if (res.success) {
                toastr.success(res.message);
                $("#jobPositionCreateUpdate").modal("hide");
                loadJobPosition();
            } else {
                toastr.warning(res.message);
            }
        } catch (err) {
             toastr.error("Không thể lưu dữ liệu!");
        }
    });

    $('#jobPositionCreateUpdate').on('hidden.bs.modal', function () {
        $('body').removeClass('modal-open');
        $('.modal-backdrop').remove();
    });

     $("#content-main").on("click", ".edit-item-btnjob", async function (e) {
        e.preventDefault();
        const id = $(this).data("id");
        if (!id || id === 0) {
            toastr.error("Không xác định được ID vị trí công tác!");
            return;
        }
        await openJobPositionModal(id);
    });

     $("#content-main").on("click", ".delete-item-btnjob", function (e) {
        e.preventDefault();
        const id = $(this).data("id");
        if (!id) return toastr.error("Không có ID để xóa!");

        if (confirm("Bạn có chắc chắn muốn xóa vị trí này không?")) {
            $.ajax({
                url: "/api/jobposition/delete",
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify({ id }),
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message);
                        $(e.currentTarget).parents("tr").remove();
                    } else {
                        toastr.warning(response.message);
                    }
                },
                error: function () {
                    toastr.error("Không thể kết nối tới server!");
                }
            });
        }
    });
});
