document.addEventListener("DOMContentLoaded", () => {
    if (!window.chatConfig) return;

    const { chatId, adId, currentUserId, otherUserId } = window.chatConfig;

    const chatBox = document.getElementById("chat-box");

    let isConnected = false;
    const imageInput = document.getElementById("imageInput");
    const imagePreview = document.getElementById("imagePreview");
    const msgInput = document.getElementById("msgInput");
    const sendBtn = document.getElementById("sendBtn");

    sendBtn.disabled = true;

    let selectedImages = [];

    imageInput.addEventListener("change", () => {
        selectedImages = Array.from(imageInput.files);
        renderPreview();
    });

    function renderPreview() {
        imagePreview.innerHTML = "";
        selectedImages.forEach((file, i) => {
            const r = new FileReader();
            r.onload = e => {
                imagePreview.innerHTML += `
                    <div class="preview-image-wrapper">
                        <img src="${e.target.result}" />
                        <span class="remove-image" onclick="removeImage(${i})">✕</span>
                    </div>`;
            };
            r.readAsDataURL(file);
        });
    }

    window.removeImage = i => {
        selectedImages.splice(i, 1);
        renderPreview();
    };

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .build();

    connection.start()
        .then(() => {
            isConnected = true;
            sendBtn.disabled = false;
            connection.invoke("JoinChat", chatId);
            chatBox.scrollTop = chatBox.scrollHeight;
        })
        .catch(err => console.error("SignalR start failed:", err));

    connection.on("ReceiveMessage", (senderId, content, imageUrls, sentOn) => {
        const mine = senderId === currentUserId;

        const div = document.createElement("div");
        div.className = `chat-message ${mine ? "mine" : "theirs"}`;

        div.innerHTML = `
            ${!mine ? `<img src="${window.otherUserAvatarUrl || '/images/default-avatar.png'}" class="chat-avatar` : ""}
            <div class="chat-bubble">
                ${imageUrls.length ? `
                    <div class="chat-images">
                        ${imageUrls.map((url, idx) =>
            `<img src="${url}" class="chat-image"
                                  onclick='openImageGallery(${JSON.stringify(imageUrls)}, ${idx})' />`
        ).join("")}
                    </div>` : ""}
                ${content ? `<div class="chat-text">${content}</div>` : ""}
                <div class="chat-time">${new Date(sentOn).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
            </div>`;

        chatBox.appendChild(div);
        chatBox.scrollTop = chatBox.scrollHeight;
    });

    sendBtn.addEventListener("click", async () => {
        if (!isConnected) return;
        if (!msgInput.value && !selectedImages.length) return;

        try {
            const urls = [];

            for (const f of selectedImages) {
                const fd = new FormData();
                fd.append("image", f);

                const r = await fetch("/Messages/UploadChatImage", {
                    method: "POST",
                    body: fd
                });

                if (!r.ok) throw new Error("Image upload failed");

                const d = await r.json();
                urls.push(d.imageUrl);
            }

            await connection.invoke(
                "SendMessage",
                adId,
                chatId,
                otherUserId,   
                msgInput.value,
                urls
            );

            msgInput.value = "";
            selectedImages = [];
            imagePreview.innerHTML = "";

        } catch (err) {
            console.error("Send failed:", err);
            alert("Message failed to send");
        }
    });

    /* image viewer */
    let gallery = [];
    let index = 0;

    window.openImageGallery = function (images, i) {
        gallery = images || [];
        index = i || 0;

        const viewer = document.getElementById("imageViewer");
        if (!viewer) return;

        viewer.classList.remove("hidden");
        updateViewer();
    };

    window.closeViewer = function () {
        document.getElementById("imageViewer")?.classList.add("hidden");
    };

    function updateViewer() {
        const img = document.getElementById("viewerImage");
        const counter = document.getElementById("imageCounter");
        if (!img || !counter || !gallery.length) return;

        img.src = gallery[index];
        counter.textContent = `${index + 1} / ${gallery.length}`;
    }

    window.prevImage = function () {
        if (index > 0) { index--; updateViewer(); }
    };

    window.nextImage = function () {
        if (index < gallery.length - 1) { index++; updateViewer(); }
    };

    // Download current image
    document.getElementById("downloadBtn")?.addEventListener("click", () => {
        if (!gallery.length) return;

        const url = gallery[index];
        const name = url.split("/").pop().split("?")[0] || "image.jpg";

        const a = document.createElement("a");
        a.href = url;
        a.download = name;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    });
});
