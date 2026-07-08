using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace gezzyn.API.Hubs
{
    [Authorize] 
    public class TripHub : Hub
    {
        private Guid CurrentUserId => Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private string CurrentUserName => Context.User!.FindFirst("UserName")?.Value ?? "Bilinmeyen";

        /// <summary>
        /// Henüz hiçbir gruba katılmadı, sadece bağlantı kuruldu.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        /// <summary>
        ///  Hangi trip grubundan ayrıldığını bilmiyoruz (Hub state tutmuyoruz)
        ///  Bu yüzden client tarafı ayrılırken LeaveTrip'i manuel çağırmalı
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        // ── Trip odasına katılma / çıkma ────────────────────────

        /// <summary>
        /// Kullanıcı bir Trip'in JAM moduna girer. Aynı TripId'ye bağlı
        /// herkes aynı "grup"ta olur, birbirlerine mesaj/güncelleme görür.
        /// </summary>
        public async Task JoinTrip(string tripId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tripId));

            await Clients.OthersInGroup(GroupName(tripId)).SendAsync("MemberJoined", new
            {
                userId = CurrentUserId,
                userName = CurrentUserName,
                connectedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Kullanıcı JAM modundan çıkar.
        /// </summary>
        public async Task LeaveTrip(string tripId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(tripId));

            await Clients.Group(GroupName(tripId)).SendAsync("MemberLeft", new
            {
                userId = CurrentUserId,
                userName = CurrentUserName
            });
        }

        public async Task NotifyTyping(string tripId)
        {
            await Clients.OthersInGroup(GroupName(tripId)).SendAsync("UserTyping", new
            {
                userId = CurrentUserId,
                userName = CurrentUserName
            });
        }

        public async Task SendMessage(string tripId, string message)
        {
            await Clients.Group(GroupName(tripId)).SendAsync("MessageReceived", new
            {
                userId = CurrentUserId,
                userName = CurrentUserName,
                message,
                sentAt = DateTime.UtcNow
            });
        }

        private static string GroupName(string tripId) => $"trip-{tripId}";
    }
}