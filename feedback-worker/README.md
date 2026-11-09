# Forts Setup feedback relay

Run `Configure-Feedback.cmd` in the author setup bundle. Requires Node.js LTS
and a Cloudflare account. Enter your bot token in its masked local prompt; never
put it in chat or source control. The wizard logs in to Cloudflare, deploys the
Worker, stores BOT_TOKEN and CHAT_ID as Worker secrets, checks readiness, and
creates `Forts-Setup-Feedback-Configured.zip` containing the EXE and public endpoint
configuration. Distribute both files together. No report is sent by the wizard.

The form is disabled until an HTTPS workers.dev endpoint is configured. Each
report sends only the text, optional contact, selected screenshot, and version/HUD
settings displayed by the app. It does not collect logs or absolute paths.

The public endpoint allows 3 requests per minute per IP and Cloudflare location;
it is not an authentication boundary or a global spam guarantee. Uploads are
stream-limited before multipart parsing and restricted to PNG/JPEG/WebP signatures
and 8 MiB. Telegram receives a document with a generic filename and then the text
as a reply. Partial delivery is reported separately. There are no automatic retries.
If abuse occurs, use Cloudflare's request rules or disable the Worker route.

Tests: `npm test`. Build validation: `npm ci && npm run check`.

API references: [Cloudflare rate limiting](https://developers.cloudflare.com/workers/runtime-apis/bindings/rate-limit/),
[Worker secrets](https://developers.cloudflare.com/workers/configuration/secrets/),
[Telegram sendDocument](https://core.telegram.org/bots/api#senddocument).
