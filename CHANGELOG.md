# Changelog

## [1.6.0](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.5.0...v1.6.0) (2026-10-04)


### Features

* add Microsoft Teams webhook report notifications ([8b9c326](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/8b9c3266516c560dc0aaf95317e69f422d211d61))
* **auth:** add configurable SSO provider connection testing ([1cdfd05](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/1cdfd0562672fac69a2b6dad501f4b8ff1586ed7))
* **auth:** add secure first-run administrator setup ([4678ed3](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/4678ed3fd1401bc4a4854405ec0b9e5b97481fd3))
* **auth:** enable active-provider SSO login and user provisioning ([236be29](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/236be291716fa065dc453b1d069a58cb9b2342ba))
* **auth:** explicitly link local administrator accounts to SSO ([d7c3711](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/d7c371181e3e834c7f8517ad2efad4d6c89041cb))
* centralize integrations with lazy-loaded team pagination ([0a169d9](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/0a169d9a35cea1f21393222723854ab8eeb06c32))
* improve management UI and webhook integrations ([2eaea2c](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/2eaea2ce4f28defdf50da6f6e2d14dd164f10111))


### Bug Fixes

* **auth:** use standard PKCE flow when optional PAR is rejected ([2774e64](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/2774e640d2c0834c9f65824281cafec642696254))

## [1.5.0](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.4.0...v1.5.0) (2026-10-02)


### Features

* **actions:** complete engineering actions v1 ([85de832](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/85de8320bd4671f7dcd2645ca74ea00c3d09d8f1))
* add engineering dashboard API and initialize Angular frontend ([9b35f16](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/9b35f16c4363ef8f2fcca9b915a4f29192018d35))
* complete Jira integration and AI semantic hardening ([f2ed43e](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/f2ed43eac5db022df9b07b299c6b1f97ce2f083b))
* **dashboard:** complete engineering dashboard v1 ([e7d4036](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/e7d4036603bc07570ac88180b3e65e4ab85fc7d1))
* harden production security and fix collapsed sidebar ([c33c412](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/c33c4127dcbe51e80893709dc021d6e29932bdf5))
* improve engineering insights navigation and reporting ([088e53b](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/088e53bf397a378bc16d4daa8a0f2eb94cca0642))
* **risks:** complete engineering risks v1 ([afdce58](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/afdce58d114309f767c611760a3960c66f1737f8))
* **team:** complete team management v1 ([8a26b99](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/8a26b991a8972ce8be718d3f0e1db75a08cbb5d0))

## [1.4.0](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.3.0...v1.4.0) (2026-09-25)


### Features

* add pagination to GitHub API client ([4075c5c](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/4075c5c43345d2bbfeeaab8c578c6adcd2260a6e))
* add retry handling for GitHub API failures ([061816a](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/061816aa7f5774de6ee5053766398cf4776720b7))
* **auth:** add JWT authentication and test auth handler ([5e30f76](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/5e30f76b23f72577eb44ad7cbd48e2b0c4a30d98))
* improve GitHub API retry and rate limit handling ([3ced157](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/3ced157fd542f3dfd8382948b81fa17aace5e799))

## [1.3.0](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.2.5...v1.3.0) (2026-09-22)


### Features

* add metric trends and early warning insights ([758f93d](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/758f93d845c27330ff7a2efe7c2ff0408f37f0d6))


### Bug Fixes

* align engineering metrics with lifecycle dates ([01840db](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/01840dba6528402f9a11cda5a5dd3b7fa7fe8f25))

## [1.2.5](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.2.4...v1.2.5) (2026-09-20)


### Bug Fixes

* align pull request metrics with lifecycle dates ([8be0081](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/8be0081056c418ac3e4e471011f17a532580ea64))
* calculate PR metrics by merge date ([44d1ceb](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/44d1ceb7104d45b4d77788e4aefe8a26678079a1))

## [1.2.4](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.2.3...v1.2.4) (2026-09-20)


### Bug Fixes

* calculate Jira lead time by completion date ([be306d1](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/be306d1a600993fc0ea26024cf93dbc70101b2d8))

## [1.2.3](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.2.2...v1.2.3) (2026-09-20)


### Bug Fixes

* serialize integration test database initialization ([3177eee](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/3177eee08702059078b696965debbd070922224d))

## [1.2.2](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.2.1...v1.2.2) (2026-09-20)


### Bug Fixes

* serialize integration test database initialization ([838ed75](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/838ed755ab690e4cd4708e7d564a4c07abb08920))

## [1.2.1](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.2.0...v1.2.1) (2026-09-20)


### Bug Fixes

* display test failures in README workflow ([d14a2c2](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/d14a2c24c76b46abc6889bfcf80b6b643d0b14fd))

## [1.2.0](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.1.0...v1.2.0) (2026-09-20)


### Features

* calculate lead time from Jira work items ([51656b9](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/51656b968ab53f687d90af0eb1742871f1abac41))

## [1.1.0](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/compare/v1.0.0...v1.1.0) (2026-09-19)


### Features

* sync GitHub engineering metrics data ([246ee6d](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/246ee6debe719fbd5eda1510ad13977b2c9000cd))

## 1.0.0 (2026-09-16)


### Features

* add AI engineering report analysis ([105ffb4](https://github.com/charlesikeng-tech/AiEngineeringManagerCopilot/commit/105ffb43e86010df8c95f9f4ba5a2b070071aaf6))
