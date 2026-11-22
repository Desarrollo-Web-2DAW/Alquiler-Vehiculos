"use strict";

const connectionStatus = document.getElementById("connection-status");

function updateConnectionStatus(message, cssClass) {
    connectionStatus.textContent = message;
    connectionStatus.className = `badge ${cssClass}`;
}

const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7252/hubs/notificaciones", {
        accessTokenFactory: async () => {
            const response = await fetch("/auth/signalr-token", {
                method: "GET",
                credentials: "same-origin",
                cache: "no-store"
            });

            if (!response.ok) {
                throw new Error("No se ha podido obtener el token JWT.");
            }

            const data = await response.json();

            return data.accessToken;
        },
        transport: signalR.HttpTransportType.LongPolling
    })
    .withAutomaticReconnect()
    .build();

connection.onreconnecting(() => {
    updateConnectionStatus("Reconectando...", "bg-warning");
});

connection.onreconnected(() => {
    updateConnectionStatus("Conectado", "bg-success");
});

connection.onclose(() => {
    updateConnectionStatus("Desconectado", "bg-secondary");
});

function addNotification(title, message, date = new Date()) {
    const container = document.getElementById("notifications-container");

    const emptyMessage = container.querySelector(".text-muted");

    if (emptyMessage) {
        emptyMessage.remove();
    }

    const notification = document.createElement("div");
    notification.className = "notification-item";

    const heading = document.createElement("h5");
    heading.textContent = title;

    const content = document.createElement("p");
    content.textContent = message;

    const timestamp = document.createElement("span");
    timestamp.className = "notification-date";
    timestamp.textContent = new Date(date).toLocaleString("es-ES");

    notification.appendChild(heading);
    notification.appendChild(content);
    notification.appendChild(timestamp);

    container.prepend(notification);
}

async function startConnection() {
    try {
        updateConnectionStatus("Conectando...", "bg-warning");

        await connection.start();

        updateConnectionStatus("Conectado", "bg-success");
    } catch (error) {
        console.error("Error al conectar con SignalR:", error);
        updateConnectionStatus("Error de conexión", "bg-danger");
    }
}

startConnection();