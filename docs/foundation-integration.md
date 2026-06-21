# Foundation Integration

The `Foundation` folder contains reusable modules used as the base architecture for YourRhythm Studio.

## Enabled Modules

- `Foundation.Core`: shared operation results, errors, clock, guards, and secure code generation.
- `Foundation.Access`: SaaS access, accounts, tenants, memberships, roles, sessions, password hashing, and registration review.
- `Foundation.SecureLinks`: secure public links and QR artifacts for future lesson materials.
- `Foundation.Assistant`: conversation and message-channel primitives for future communication and assistant features.

`Foundation.Freight` is kept in the repository for completeness, but it is not referenced by the web app because freight is outside the current music-education domain.

## YourRhythm Mapping

- Tenant: school or music studio.
- Account: user identity.
- Tenant membership: relation between a user and a school.
- Foundation tenant role: base access role for owner, admin, billing, or member.
- YourRhythm role constants: product-level roles such as teacher and student.
- Foundation features: plan-gated modules such as students, repertoire, materials, lessons, and gamification.

The current integration uses in-memory Foundation stores. They are suitable for architecture wiring and prototypes, not production persistence.
