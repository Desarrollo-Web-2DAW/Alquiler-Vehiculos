
"use strict";

const connectionStatus = document.getElementById("connection-status");
const notificacionesPage = document.getElementById("notificaciones-page");
const notificationsContainer = document.getElementById("notifications-container");

let reconnectTimer = null;

function updateConnectionStatus(message, cssClass) {
    connectionStatus.textContent = message;
    connectionStatus.className = `badge ${cssClass}`;
}

function scheduleReconnect() {
    if (reconnectTimer !== null) {
        return;
    }

    reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        startConnection();
    }, 5000);
}

function addNotification(title, message, date = new Date()) {
    const emptyMessage = notificationsContainer.querySelector(".text-muted");

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

    notificationsContainer.prepend(notification);
}

function addGenericNotification(title, data) {
    addNotification(
        title,
        data == null ? "Notificación recibida." : JSON.stringify(data),
        new Date()
    );
}

const connection = new signalR.HubConnectionBuilder()
    .withUrl(notificacionesPage.dataset.hubUrl, {
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

// Eventos de conexión.

connection.onreconnecting(() => {
    updateConnectionStatus("Reconectando...", "bg-warning");
});

connection.onreconnected(() => {
    updateConnectionStatus("Conectado", "bg-success");
});

connection.onclose(() => {
    updateConnectionStatus("Desconectado. Reintentando...", "bg-warning");
    scheduleReconnect();
});

// Eventos de taller.

connection.on("VehiculoEntradaTaller", (data) => {
    addNotification(
        "Vehículo entrado en taller",
        `El vehículo ${data.vehiculoId} ha entrado en el taller. Trabajo: ${data.descripcion}`,
        data.fecha
    );
});

connection.on("VehiculoSalidaTaller", (data) => {
    addNotification(
        "Vehículo salido del taller",
        `El vehículo ${data.vehiculoId} ha salido del taller. Trabajo: ${data.descripcion}`,
        data.fecha
    );
});

// Eventos de reservas y disponibilidad.
// Los datos se muestran de forma genérica hasta que existan
// los contratos definitivos de sus emisores.

connection.on("ReservaCreada", (data) => {
    addGenericNotification("Nueva reserva", data);
});

connection.on("EstadoReservaCambiado", (data) => {
    addGenericNotification("Estado de reserva actualizado", data);
});

connection.on("VehiculoDisponible", (data) => {
    addGenericNotification("Vehículo disponible", data);
});

connection.on("ReservaCaducada", (data) => {
    addGenericNotification("Reserva caducada", data);
});

// Inicio y recuperación de la conexión.

async function startConnection() {
    if (connection.state !== signalR.HubConnectionState.Disconnected) {
        return;
    }

    try {
        updateConnectionStatus("Conectando...", "bg-warning");

        await connection.start();

        updateConnectionStatus("Conectado", "bg-success");
    } catch (error) {
        console.error("Error al conectar con SignalR:", error);

        updateConnectionStatus("Error de conexión. Reintentando...", "bg-warning");

        scheduleReconnect();
    }
}

startConnection();
