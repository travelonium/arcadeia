/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

public class SessionMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;
    private const string SessionStartedKey = "Started";
    private const string CookieName = "SessionStarted";

    public async Task InvokeAsync(HttpContext context)
    {
        string? sessionTimestamp = context.Session.GetString(SessionStartedKey);
        string? clientCookie = context.Request.Cookies[CookieName];

        if (string.IsNullOrEmpty(sessionTimestamp))
        {
            // New session: Generate timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            context.Session.SetString(SessionStartedKey, timestamp);
            sessionTimestamp = timestamp;
        }

        // If the session exists but the cookie was removed, restore it
        if (!string.IsNullOrEmpty(sessionTimestamp) && string.IsNullOrEmpty(clientCookie))
        {
            context.Response.Cookies.Append(CookieName, sessionTimestamp, new CookieOptions
            {
                HttpOnly = false,   // Allows frontend access
                IsEssential = true, // Ensures the cookie is always sent
                SameSite = SameSiteMode.Strict
            });
        }

        await _next(context);
    }
}
