$(function () {
    let currentDeptId = null;

    $(document).off("click", "#btnAdd").on("click", "#btnAdd", function () {
        openDepartmentModal(0);
    });


     function loadDepartment(filters) {
        $.ajax({
            url: '/api/departments/name',
            type: 'GET',
            data: filters,
            success: function (res) {
                console.log("API Response:", res);

                if (res.success && res.data && res.data.length > 0) {
                    $("#noData").addClass("d-none");
                    renderDepartmentList(res.data);
                } else {
                    $("#departmentList").empty();
                    $("#noData").removeClass("d-none");
                }
            },
            error: function (xhr) {
                toastr.error("Server error: " + xhr.status);
            }
        });
    }

     function renderDepartmentList(items) {
        var $list = $("#departmentList");
        $list.empty();

        items.forEach(function (item, index) {
             const statusBadge = item.status === 1
                ? '<span class="badge bg-success">Kích hoạt</span>'
                : '<span class="badge bg-warning text-dark">Tạm khóa</span>';

            const html = `
            <tr>
                <td class="text-center"><input type="checkbox" class="form-check-input"></td>
                <td><span class="fw-bold text-primary">${index + 1}</span></td>
                <td><span class="fw-medium">PB00${item.id}</span></td>
                <td><span class="fw-medium">${item.name}</span></td>
             <td class="text-center">
                <span class="fw-medium text-dark">
                    ${item.managerName || "Chưa có"}
                </span>
            </td>

             <td class="text-center">
                <i class="ri-team-line text-primary me-1"></i>
                <span class="fw-semibold">${item.employeeCount ?? 0}</span>
            </td>
                <td class="text-center">
                    ${statusBadge}
                </td>
                <td class="text-center">
                    <div class="dropup dropdown-action">
                        <a href="#" class="btn btn-soft-primary btn-sm dropdown" data-bs-toggle="dropdown" aria-expanded="false">
                            <i class="ri-more-2-fill"></i>
                        </a>
                        <ul class="dropdown-menu dropdown-menu-end">
                            <li>
                               <a href="#" class="dropdown-item view-item-btn text-primary" data-id="${item.id}">
                                    <i class="ri-eye-fill fs-16"></i> Xem chi tiết 
                                </a>

                            </li>
                            <li>
                                <a href="#" class="dropdown-item edit-item-btn text-warning" data-id="${item.id}">
                                    <i class="ri-edit-fill fs-16"></i> Chỉnh sửa
                                </a>
                            </li>
                            <li>
                                <a href="#" class="dropdown-item delete-item-btn text-danger" data-id="${item.id}">
                                     <i class="ri-delete-bin-5-fill fs-16"></i> Xóa bỏ
                                </a>
                            </li>
                        </ul>
                    </div>
                </td>
            </tr>
        `;
            $list.append(html);
        });
    }
     async function loadJobPositions(selectedId = null) {
        try {
            const res = await $.getJSON("/api/jobpositions/list");
            const ddl = $("#jobPositionSelect");
            ddl.empty();
            ddl.append(`<option value="">-- Chọn chức vụ --</option>`);

            if (res.success && res.data.length > 0) {
                res.data.forEach(p => {
                    ddl.append(`<option value="${p.id}">${p.name}</option>`);
                });
                if (selectedId) ddl.val(selectedId);
            } else {
                ddl.append(`<option disabled>Không có dữ liệu</option>`);
            }
        } catch (err) {
            console.error("Lỗi load chức vụ:", err);
        }
    }

     $("#content-main").on("click", ".view-item-btn", async function (e) {
        e.preventDefault();
        const id = $(this).data("id");

        if (!id) {
            toas.error = "Không xác định được Id phòng ban";
            return;
        }

        try {
            const res = await $.ajax({
                url: `/api/department/view/${id}`,
                type: "GET",
                dataType: "json"
            });

            if (res.success && res.data) {
                const d = res.data;

                $("#detailDepartmentCode").val(d.code || "—");
                $("#detailDepartmentName").val(d.name || "-");
                $("#detailManagerName").val(d.manager?.fullname || "Chưa có");
                $("#detailJobPositionName").val(d.jobPosition?.name || "—");
                $("#detailDepartmentKeyword").val(d.keyword || "-");
                $("#detailStatusText").val(d.status === 1 ? "Kích hoạt" : "Tạm khóa");


                $("#detailCreatedBy").val(d.createBy ? d.createBy.fullName : "—");
                $("#detailCreatedDate").val(d.createDate ? new Date(d.createDate).toLocaleString() : "—");
                $("#detailUpdatedBy").val(d.updateBy ? d.updateBy.fullName : "-");
                $("#detailUpdatedDate").val(d.updatedDate ? new Date(d.updatedDate).toLocaleString() : "-");

                const modal = new bootstrap.Modal(document.getElementById("departmentDetailModal"));
                modal.show();

            }
            else {
                toas.warning(res.message || "Không tìm thấy phòng ban");
            }
        }
        catch (err) {
            console.error("Không thể tìm thấy phòng ban" + err);
            console.error("Không thể kết nối đến server" + err);

        }

    });

    $("#content-main").on("click", ".delete-item-btn", function (e) {
        e.preventDefault();
        const id = $(this).data("id");
        console.log("Click delete, id=", id);

        if (!id) {
            toastr.error("Không có ID để xóa!");
            return;
        }

        if (confirm("Bạn có chắc chắn muốn xóa bài viết này không?")) {
            $.ajax({
                url: "/api/department/delete",
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


    $("#content-main").on("click", ".view-item-btn", function (e) {
        e.preventDefault();
        currentDeptId = $(this).data("id");

        if (!currentDeptId) {
            toastr.error("Không xác định được ID phòng ban!");
            return;
        }

        const modalEl = document.getElementById("departmentDetailModal");
        const modal = new bootstrap.Modal(modalEl);


    });

    async function openDepartmentModal(id = 0) {
        const modal = new bootstrap.Modal(document.getElementById("DepartmentCreateUpdate"));

        $("#departmentForm")[0].reset();
        $("#managerSelect").prop("disabled", true);
        $("#departmentId").val(id || 0);

        if (id === 0) {
             $("#createdBy").val(window.currentUserName || "Không xác định");
            $("#createdDate").val(new Date().toLocaleString());
            $("#updatedBy").val("");
            $("#updatedDate").val("");
            await loadJobPositions();  

            modal.show();
            return;
        }

         const res = await $.getJSON(`/api/department/view/${id}`);
        if (res.success && res.data) {
            const d = res.data;

            $("#departmentId").val(d.id);
            $("#DepartmentCode").val(d.code);
            $("#departmentName").val(d.name);
            $("#departmentKeyword").val(d.keyword || "");  
            $("#jobPositionSelect").val(d.jobPosition?.id || "");

            $("#statusSelect").val(d.status);

            $("#createdBy").val(d.createBy?.fullName || "—");
            $("#createdDate").val(d.createDate ? new Date(d.createDate).toLocaleString() : "");
            $("#updatedBy").val(window.currentUserName || "Không xác định");
            $("#updatedDate").val(new Date().toLocaleString());

            const empRes = await $.getJSON(`/api/employees/by-department/${id}`);
            const ddl = $("#managerSelect");
            ddl.empty();
            ddl.append(`<option value="">-- Chọn Trưởng phòng --</option>`);
            empRes.data.forEach(e => ddl.append(`<option value="${e.id}">${e.fullname}</option>`));
            ddl.prop("disabled", false);

            if (d.manager?.id)
                ddl.val(d.manager.id);
            await loadJobPositions(d.jobPosition?.id);  
            modal.show();
        }
    }




    $(document).off("click", "#btnSaveDepartment").on("click", "#btnSaveDepartment", async function () {
 
        const id = parseInt($("#departmentId").val()) || 0;
        const statusVal = $("#statusSelect").val();
        const managerVal = $("#managerSelect").val();
        const jobPosVal = $("#jobPositionSelect").val(); 

        const model = {
            Id: id,
            Code: $("#DepartmentCode").val().trim(),
            Name: $("#departmentName").val().trim(),
            Keyword: $("#departmentKeyword").val().trim(),  
            JobPositionId: jobPosVal ? parseInt(jobPosVal) : null,  

            Status: statusVal ? parseInt(statusVal) : 1,
            ManagerId: managerVal ? parseInt(managerVal) : null,
            CreateBy: id === 0 ? parseInt(window.currentUserId) : null,
            UpdatedBy: id > 0 ? parseInt(window.currentUserId) : null
        };

        if (!model.Code || !model.Name) {
            toastr.warning("Vui lòng nhập đầy đủ Mã phòng và Tên phòng!");
            return;
        }

        try {
            const res = await $.ajax({
                url: "/api/department/save",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(model)
            });

            if (res.success) {
                toastr.success(res.message);
                $("#DepartmentCreateUpdate").modal("hide");
                loadDepartment();
            } else {
                toastr.warning(res.message);
            }
        } catch (err) {
            console.error("Lỗi lưu phòng ban:", err);
            toastr.error("Không thể lưu dữ liệu!");
        }
    });



    $("#content-main").on("click", ".edit-item-btn", async function (e) {
        e.preventDefault();
        const id = $(this).data("id");

        if (!id || id === 0) {
            toastr.error("Không xác định được ID phòng ban!");
            return;
        }

         await openDepartmentModal(id);
    });









    // Gọi lần đầu
    loadDepartment();





});
