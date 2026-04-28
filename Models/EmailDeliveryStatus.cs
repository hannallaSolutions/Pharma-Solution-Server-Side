public enum EmailDeliveryStatus
{
    // ── Pre-send, caught locally ─────────────────────────────
    InvalidFormat,           // Bad syntax — never reaches external API
    InvalidDomain,           // No MX record found — never reaches external API
    DisposableEmail,         // Throwaway domain — never reaches external API

    // ── External verification outcomes ──────────────────────
    VerifiedDeliverable,     // External API explicitly confirmed deliverable
    RiskyOrUnknown,          // External API could not confirm mailbox existence
                             // (catch-all domain, greylisted, SMTP blocked, etc.)

    // ── Post-send outcomes ───────────────────────────────────
    AcceptedLocalOnly,       // External verification off; passed local checks only
    SendFailed               // SMTP/network error during actual send attempt
}