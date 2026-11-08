
"use strict";

(function () {
  const statuses = {
    Pending: "Mới tạo",
    InProgress: "Đang xử lý",
    Resolved: "Đã xử lý",
    Closed: "Đã đóng"
  };

  const e = (s) =>
    String(s ?? "").replace(/[&<>"']/g, (c) => ({
      "&": "&amp;",
      "<": "&lt;",
      ">": "&gt;",
      '"': "&quot;",
      "'": "&#39;"
    }[c]));

  const el = (id) => document.getElementById(id);

  const date = (val) =>
    val
      ? new Date(val).toLocaleString("vi-VN", {
          year: "numeric",
          month: "2-digit",
          day: "2-digit",
          hour: "2-digit",
          minute: "2-digit"
        })
      : "—";

  const code = (id) =>
    "PF" + String(id).padStart(4, "0");

  const notice = (id, txt) => {
    const node = el(id);

    if (node) {
      node.hidden = false;
      node.textContent = txt;
    }
  };

  const clearNotice = (id) => {
    const node = el(id);

    if (node) {
      node.hidden = true;
      node.textContent = "";
    }
  };

  const route = (id) =>
    "/Student/Details?id=" + encodeURIComponent(id);

  const formatErr = (err) =>
    String(
      err?.message || err || "Có lỗi xảy ra."
    ).slice(0, 500);

  async function get(url) {
    const res = await fetch(url, {
      credentials: "same-origin",
      headers: {
        Accept: "application/json"
      }
    });

    if (!res.ok) {
      throw new Error(
        res.status === 401
          ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
          : `Lỗi kết nối (${res.status}).`
      );
    }

    return res.json();
  }

  async function readErrorResponse(response, fallback) {
    let body = null;

    try {
      body = await response.json();
    } catch {
      // Một số lỗi máy chủ không trả về JSON.
    }

    if (response.status === 401) {
      return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    }

    if (response.status === 403) {
      return "Bạn không có quyền thực hiện thao tác này.";
    }

    return body?.message ||
      body?.title ||
      `${fallback} (HTTP ${response.status}).`;
  }

  async function data() {
    return get("/api/reflections");
  }

  async function categories() {
    return get("/api/categories");
  }

  function stat(id, n) {
    if (el(id)) {
      el(id).textContent = n;
    }
  }

  // =====================================================
  // TRANG CHỦ SINH VIÊN
  // =====================================================

  async function initHome() {
    try {
      const [rows, responses] = await Promise.all([
        data(),
        get("/api/responses")
      ]);

      stat("totalCount", rows.length);

      stat(
        "progressCount",
        rows.filter(
          (x) => x.status === "InProgress"
        ).length
      );

      stat(
        "resolvedCount",
        rows.filter(
          (x) =>
            x.status === "Resolved" ||
            x.status === "Closed"
        ).length
      );

      stat(
        "pendingCount",
        rows.filter(
          (x) => x.status === "Pending"
        ).length
      );

      const box = el("responseList");

      if (!box) {
        return;
      }

      box.innerHTML = !responses.length
        ? '<p class="muted-empty">Chưa có phản hồi từ quản trị viên.</p>'
        : responses.slice(0, 5).map((r) =>
            '<article class="sf-response">' +
              "<div>" +
                "<strong>Phản hồi về " +
                  e(code(r.reflectionId)) +
                "</strong>" +
                "<small>" +
                  e(date(r.createdAt)) +
                "</small>" +
              "</div>" +
              "<p>" + e(r.content) + "</p>" +
              '<a href="' + route(r.reflectionId) + '">' +
                "Xem phản ánh →" +
              "</a>" +
            "</article>"
          ).join("");

    } catch (err) {
      notice("loadError", formatErr(err));

      [
        "totalCount",
        "progressCount",
        "resolvedCount",
        "pendingCount"
      ].forEach((id) => stat(id, "—"));

      if (el("responseList")) {
        el("responseList").textContent =
          "Không tải được phản hồi.";
      }
    }
  }

  // =====================================================
  // DANH SÁCH PHẢN ÁNH
  // =====================================================

  async function initList() {
    let all = [];
    let cats = [];

    let status =
      new URLSearchParams(location.search)
        .get("status") || "";

    let page = 1;

    const query =
      new URLSearchParams(location.search)
        .get("search") || "";

    el("searchTerm").value = query;

    function render() {
      const search =
        el("searchTerm").value
          .trim()
          .toLocaleLowerCase("vi");

      const categoryId =
        el("categoryFilter").value;

      const rows = all.filter((r) =>
        (!status || r.status === status) &&
        (!categoryId ||
          String(r.categoryId) === categoryId) &&
        (!search ||
          (
            r.title + " " +
            r.content + " " +
            r.id + " " +
            code(r.id)
          ).toLocaleLowerCase("vi").includes(search))
      );

      const pages =
        Math.max(1, Math.ceil(rows.length / 10));

      page = Math.min(page, pages);

      const show = rows.slice(
        (page - 1) * 10,
        page * 10
      );

      el("reflectionRows").innerHTML = show.length
        ? show.map((r) =>
            "<tr>" +
              "<td><strong>" +
                code(r.id) +
              "</strong></td>" +
              "<td>" + e(r.title) + "</td>" +
              "<td>" +
                e(
                  cats.find(
                    (c) => c.id === r.categoryId
                  )?.name || "Chưa phân loại"
                ) +
              "</td>" +
              '<td><span class="state ' +
                e(r.status) + '">' +
                e(statuses[r.status] || r.status) +
              "</span></td>" +
              "<td>" +
                e(date(r.createdAt)) +
              "</td>" +
              '<td><a href="' +
                route(r.id) +
                '">Xem chi tiết →</a></td>' +
            "</tr>"
          ).join("")
        : '<tr><td colspan="6" class="empty-table">' +
            "Không có phản ánh phù hợp." +
          "</td></tr>";

      el("listCount").textContent =
        `Hiển thị ${show.length}/${rows.length} phản ánh`;

      el("listPaging").innerHTML =
        Array.from(
          { length: pages },
          (_, i) =>
            '<button type="button" class="' +
              (page === i + 1 ? "active" : "") +
              '" data-page="' +
              (i + 1) +
              '">' +
              (i + 1) +
            "</button>"
        ).join("");

      el("listPaging")
        .querySelectorAll("button")
        .forEach((button) => {
          button.addEventListener("click", () => {
            page = Number(button.dataset.page);
            render();
          });
        });

      document
        .querySelectorAll("#statusTabs button")
        .forEach((button) => {
          button.classList.toggle(
            "active",
            button.dataset.status === status
          );
        });
    }

    el("listFilters").addEventListener(
      "submit",
      (ev) => {
        ev.preventDefault();
        page = 1;
        render();
      }
    );

    el("searchTerm").addEventListener(
      "input",
      () => {
        page = 1;
        render();
      }
    );

    el("categoryFilter").addEventListener(
      "change",
      () => {
        page = 1;
        render();
      }
    );

    document
      .querySelectorAll("#statusTabs button")
      .forEach((button) => {
        button.addEventListener("click", () => {
          status = button.dataset.status || "";
          page = 1;
          render();
        });
      });

    try {
      [all, cats] = await Promise.all([
        data(),
        categories()
      ]);

      el("categoryFilter").innerHTML =
        '<option value="">Tất cả danh mục</option>' +
        cats.map(
          (c) =>
            '<option value="' +
              c.id +
              '">' +
              e(c.name) +
            "</option>"
        ).join("");

      render();

    } catch (err) {
      el("reflectionRows").innerHTML = "";

      notice(
        "listMessage",
        formatErr(err)
      );

      el("listCount").textContent =
        "Không tải được dữ liệu";
    }
  }

  // =====================================================
  // TẠO PHẢN ÁNH VÀ UPLOAD FILE MINIO
  // =====================================================

  async function initCreate() {
    const form = el("feedbackCreate");
    const content = el("contentInput");
    const fileInput = el("attachmentInput");
    const fileList = el("attachmentList");
    const button = el("submitReflection");

    if (!form || !content || !fileInput || !button) {
      return;
    }

    const MAX_FILES = 5;
    const MAX_FILE_SIZE = 10 * 1024 * 1024;

    const allowedExtensions = [
      ".pdf",
      ".jpg",
      ".jpeg",
      ".png"
    ];

    let submitting = false;

    content.addEventListener("input", () => {
      stat("textLength", content.value.length);
    });

    function validateFiles(files) {
      if (files.length > MAX_FILES) {
        throw new Error(
          "Mỗi phản ánh chỉ được đính kèm tối đa 5 tệp."
        );
      }

      for (const file of files) {
        const name = file.name || "";

        const extension =
          name.substring(name.lastIndexOf("."))
            .toLowerCase();

        if (!allowedExtensions.includes(extension)) {
          throw new Error(
            `Tệp "${name}" không được hỗ trợ. ` +
            "Chỉ chấp nhận PDF, JPG, JPEG và PNG."
          );
        }

        if (name.length > 255) {
          throw new Error(
            `Tên tệp "${name}" quá dài.`
          );
        }

        if (
          file.size <= 0 ||
          file.size > MAX_FILE_SIZE
        ) {
          throw new Error(
            `Tệp "${name}" phải có dung lượng ` +
            "lớn hơn 0 và không quá 10 MB."
          );
        }
      }
    }

    function renderFiles() {
      if (!fileList) {
        return;
      }

      const files = Array.from(fileInput.files || []);

      if (!files.length) {
        fileList.textContent =
          "Chưa chọn tệp minh chứng.";
        return;
      }

      fileList.innerHTML =
        "<strong>Đã chọn " +
          files.length +
          " tệp:</strong>" +
        "<ul style='margin-top:8px;'>" +
          files.map((file) =>
            "<li>" +
              e(file.name) +
              " (" +
              (file.size / 1024 / 1024)
                .toFixed(2) +
              " MB)" +
            "</li>"
          ).join("") +
        "</ul>";
    }

    fileInput.addEventListener("change", () => {
      clearNotice("createMessage");

      const files = Array.from(
        fileInput.files || []
      );

      try {
        validateFiles(files);
      } catch (err) {
        notice(
          "createMessage",
          formatErr(err)
        );

        fileInput.value = "";
      }

      renderFiles();
    });

    renderFiles();

    try {
      const cats = await categories();

      el("categoryInput").innerHTML =
        '<option value="">Chọn danh mục</option>' +
        cats.map(
          (c) =>
            '<option value="' +
              c.id +
              '">' +
              e(c.name) +
            "</option>"
        ).join("");

    } catch (err) {
      notice(
        "createMessage",
        formatErr(err)
      );
    }

    form.addEventListener(
      "submit",
      async (ev) => {
        ev.preventDefault();

        if (submitting) {
          return;
        }

        clearNotice("createMessage");

        const title =
          el("titleInput").value.trim();

        const body =
          content.value.trim();

        const cid =
          Number(el("categoryInput").value);

        const files =
          Array.from(fileInput.files || []);

        if (!title || !body || !cid) {
          notice(
            "createMessage",
            "Vui lòng nhập tiêu đề, nội dung " +
            "và chọn danh mục."
          );
          return;
        }

        if (
          title.length > 200 ||
          body.length > 4000
        ) {
          notice(
            "createMessage",
            "Tiêu đề hoặc nội dung vượt quá " +
            "độ dài cho phép."
          );
          return;
        }

        try {
          validateFiles(files);
        } catch (err) {
          notice(
            "createMessage",
            formatErr(err)
          );
          return;
        }

        submitting = true;
        button.disabled = true;
        button.textContent = "Đang tạo phản ánh…";

        let createdId = null;

        try {
          // Lấy antiforgery token cho cả hai API.
          const tok = await get(
            "/api/antiforgery/token"
          );

          if (!tok?.requestToken) {
            throw new Error(
              "Không lấy được mã xác thực bảo mật."
            );
          }

          // BƯỚC 1: Tạo phản ánh.
          const response = await fetch(
            "/api/reflections",
            {
              method: "POST",
              credentials: "same-origin",
              headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken":
                  tok.requestToken,
                Accept: "application/json"
              },
              body: JSON.stringify({
                title: title,
                content: body,
                categoryId: cid
              })
            }
          );

          if (!response.ok) {
            throw new Error(
              await readErrorResponse(
                response,
                "Không thể tạo phản ánh"
              )
            );
          }

          const created = await response.json();

          createdId = Number(created?.id);

          if (
            !Number.isInteger(createdId) ||
            createdId <= 0
          ) {
            throw new Error(
              "Phản ánh đã được gửi nhưng máy chủ " +
              "không trả về mã phản ánh hợp lệ. " +
              "Hãy kiểm tra danh sách phản ánh " +
              "trước khi gửi lại."
            );
          }

          // Không có tệp thì hoàn thành ngay.
          if (!files.length) {
            location.href = route(createdId);
            return;
          }

          // BƯỚC 2: Upload lần lượt từng tệp.
          const failures = [];

          for (
            let index = 0;
            index < files.length;
            index++
          ) {
            const file = files[index];

            button.textContent =
              `Đang tải tệp ${index + 1}/${files.length}…`;

            const formData = new FormData();

            // Tên trường phải đúng với backend.
            formData.append(
              "ReflectionId",
              String(createdId)
            );

            formData.append(
              "File",
              file,
              file.name
            );

            try {
              const uploadResponse = await fetch(
                "/api/attachments",
                {
                  method: "POST",
                  credentials: "same-origin",
                  headers: {
                    "RequestVerificationToken":
                      tok.requestToken,
                    Accept: "application/json"
                  },
                  body: formData
                }
              );

              if (!uploadResponse.ok) {
                throw new Error(
                  await readErrorResponse(
                    uploadResponse,
                    "Không thể tải tệp"
                  )
                );
              }

            } catch (uploadError) {
              failures.push({
                name: file.name,
                error: formatErr(uploadError)
              });
            }
          }

          // BƯỚC 3: Xử lý kết quả upload.
          if (failures.length) {
            const failedNames = failures
              .map((item) => item.name)
              .join(", ");

            notice(
              "createMessage",
              `Phản ánh ${code(createdId)} đã được tạo. ` +
              `Tuy nhiên ${failures.length}/${files.length} ` +
              `tệp tải lên thất bại: ${failedNames}. ` +
              `Lý do: ${failures[0].error}. ` +
              "Vui lòng kiểm tra phản ánh trước khi " +
              "thử lại. Không nhấn Gửi phản ánh lần nữa."
            );

            // Ngăn tạo phản ánh trùng.
            button.textContent =
              "Phản ánh đã được tạo";

            const detailLink =
              document.createElement("a");

            detailLink.href = route(createdId);
            detailLink.textContent =
              "Xem phản ánh vừa tạo →";

            detailLink.style.display = "inline-block";
            detailLink.style.marginTop = "12px";
            detailLink.style.fontWeight = "700";

            const message = el("createMessage");

            if (message) {
              message.appendChild(
                document.createElement("br")
              );

              message.appendChild(detailLink);
            }

            return;
          }

          // Tất cả tệp đã upload thành công.
          location.href = route(createdId);

        } catch (err) {
          notice(
            "createMessage",
            formatErr(err)
          );

          if (createdId) {
            button.textContent =
              "Phản ánh đã được tạo";

            return;
          }

          button.disabled = false;
          button.textContent =
            "Gửi phản ánh →";

          submitting = false;
        }
      }
    );
  }

  // =====================================================
  // CHI TIẾT PHẢN ÁNH
  // =====================================================

  async function initDetails() {
    const id = Number(
      new URLSearchParams(location.search)
        .get("id")
    );

    if (!Number.isInteger(id) || id <= 0) {
      notice(
        "detailError",
        "Mã phản ánh không hợp lệ."
      );
      return;
    }

    try {
      const [item, cats, rs] = await Promise.all([
        get("/api/reflections/" + id),
        categories(),
        get("/api/responses?reflectionId=" + id)
      ]);

      stat(
        "detailTitle",
        code(item.id) + " – " + item.title
      );

      stat(
        "detailStatus",
        statuses[item.status] || item.status
      );

      el("detailStatus").className =
        "state " + item.status;

      stat(
        "detailCategory",
        cats.find(
          (x) => x.id === item.categoryId
        )?.name || "—"
      );

      stat(
        "detailCreated",
        date(item.createdAt)
      );

      stat(
        "detailUpdated",
        date(item.updatedAt || item.createdAt)
      );

      stat(
        "detailContent",
        item.content
      );

      el("detailResponses").innerHTML =
        rs.length
          ? rs.map((r) =>
              '<article class="sf-response">' +
                "<div>" +
                  "<strong>Quản trị viên</strong>" +
                  "<small>" +
                    e(date(r.createdAt)) +
                  "</small>" +
                "</div>" +
                "<p>" +
                  e(r.content) +
                "</p>" +
              "</article>"
            ).join("")
          : '<p class="muted-empty">' +
              "Chưa có phản hồi từ quản trị viên." +
            "</p>";

    } catch (err) {
      notice(
        "detailError",
        formatErr(err)
      );

      el("detailResponses").textContent =
        "Không tải được dữ liệu.";
    }
  }

  // =====================================================
  // EXPORT
  // =====================================================

  window.StudentUI = {
    initHome,
    initList,
    initCreate,
    initDetails
  };
})();
