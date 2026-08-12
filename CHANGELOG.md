BuildTestConsole Changelog
<a name="2.4.1"></a>
## [2.4.1](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v2.4.1) (2026-08-12)

### Continuous Integration

* Specify the project for the publish ([b48cff6](https://www.github.com/jeffu231/FlexRadioServices/commit/b48cff6ce39c4bf58da6d0f29a5751bbdbd943a5))
* Update action versions ([2a1ba0c](https://www.github.com/jeffu231/FlexRadioServices/commit/2a1ba0cfba87f7af64ce813752deb48103df17b7))

<a name="2.4.0"></a>
## [2.4.0](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v2.4.0) (2026-08-12)

### Features

* **Config:** Validate runtime configuration ([2d7502c](https://www.github.com/jeffu231/FlexRadioServices/commit/2d7502cf020ce0d6ceef91700c7cea998d9d0951))
* **FlexLib:** Manage host lifecycle ([d917e80](https://www.github.com/jeffu231/FlexRadioServices/commit/d917e806c6a088fc39b484969448451e2c0b3ce9))
* **MQTT:** Supervise broker lifecycle ([5661a09](https://www.github.com/jeffu231/FlexRadioServices/commit/5661a09016521ce42e65c4aa02ea7d895840ed9d))
* **MQTT:** Supervise radio event publishing ([d0b645a](https://www.github.com/jeffu231/FlexRadioServices/commit/d0b645acd7979a8f53864a4e92f54b56b66d80bb))

### Bug Fixes

* Enusre property changed event is clean up on radio remove ([07cd518](https://www.github.com/jeffu231/FlexRadioServices/commit/07cd51897834ded9050dd7fcd1e4abfa21eac6c4))
* Fix leaking event handlers in the radio manager service ([d25c1f4](https://www.github.com/jeffu231/FlexRadioServices/commit/d25c1f4c25f9fca463aa6d3e5b07c6f7cb1633d0))
* **API:** Redact MQTT credentials ([e114b96](https://www.github.com/jeffu231/FlexRadioServices/commit/e114b961c5386d6bfee7f44dffedab4d33e3f9e5))
* **API:** Version spot response contracts ([8b5fad0](https://www.github.com/jeffu231/FlexRadioServices/commit/8b5fad03026dfda5d5484532d0ba21c6bd31c906))
* **CAT:** Correct secondary slice selection ([f45f642](https://www.github.com/jeffu231/FlexRadioServices/commit/f45f64295045e1ae56013c793fb10de55fa506f3))
* **CAT:** Isolate client command framing ([cf7aa01](https://www.github.com/jeffu231/FlexRadioServices/commit/cf7aa012b79aad7fa12697153b8a47b96772f04b))
* **docker:** Allow a specific trusted-LAN interface without changing the Compose file ([011df3d](https://www.github.com/jeffu231/FlexRadioServices/commit/011df3dbd5c1cc6884041bf5085bb44b416e4bfd))
* **openapi:** enforce advisory checks ([116082c](https://www.github.com/jeffu231/FlexRadioServices/commit/116082c544e2e8ec66a45d323f8271d4c36f3d96))
* **Radio:** Guard disappearing radio events ([550d84c](https://www.github.com/jeffu231/FlexRadioServices/commit/550d84c2e3b8d43f076c2224a9fa662c4eae8c4e))
* **Radio:** Validate slice patch before commit ([ed55dee](https://www.github.com/jeffu231/FlexRadioServices/commit/ed55dee6e7b0686c5d095f2e47d13fd6119d31a1))
* **tcp:** make CAT shutdown deterministic ([f16aede](https://www.github.com/jeffu231/FlexRadioServices/commit/f16aedeaa63d20ecb84dc6666b9a2d54308f2608))

### Continuous Integration

* **git:** Add user appsettings to git ignore ([47ff8b4](https://www.github.com/jeffu231/FlexRadioServices/commit/47ff8b46ae55ab702680a368eed62e4e91083c48))

### Documentation

* Add agent skills ([1b96d65](https://www.github.com/jeffu231/FlexRadioServices/commit/1b96d655fc77a17ce8e797ee618f1b602c8a32ab))

<a name="2.3.1"></a>
## [2.3.1](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v2.3.1) (2026-03-18)

### Bug Fixes

* **MQTT:** Fix bug where mox bool values where published in upper case ([e71a4c8](https://www.github.com/jeffu231/FlexRadioServices/commit/e71a4c8d474dbaed5ca9fb090473053767a15fcb))

<a name="2.3.0"></a>
## [2.3.0](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v2.3.0) (2026-03-11)

### Features

* **API:** Add Mode List to slice model ([3e584ba](https://www.github.com/jeffu231/FlexRadioServices/commit/3e584baafbdbbf874ded8553a2dfd33ad9da0649))
* **Json:** Replace last references to Newtonsoft / Bump to NET 10 ([3824c8c](https://www.github.com/jeffu231/FlexRadioServices/commit/3824c8c1c5e328d32adb381111620dfcac6e4e0d))

### Bug Fixes

* Remove publishing empty tx info and fix log message level ([b0c05ee](https://www.github.com/jeffu231/FlexRadioServices/commit/b0c05ee40029d481112c1c3c20884ca7e802c974))
* **API:** Fix incorrect logic in slices API giving radio not connected error ([9a3e72b](https://www.github.com/jeffu231/FlexRadioServices/commit/9a3e72bae9ee12cdcbea1cb4fc22730fad7b5b90))
* **CI:** Update publish to NET 10 ([9e0bb75](https://www.github.com/jeffu231/FlexRadioServices/commit/9e0bb759ff355d6e26399c60833ebfd5a4bbab40))
* **JsonSerializer:** Convert to using System.Text.Json ([f4c5ab8](https://www.github.com/jeffu231/FlexRadioServices/commit/f4c5ab88a144b3843bc7e970c657ec9e30d50879))

<a name="2.2.0"></a>
## [2.2.0](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v2.2.0) (2026-03-11)

### Features

* **Configuration:** Add a configuration option to control Full Duplex Mute feature ([ac878eb](https://www.github.com/jeffu231/FlexRadioServices/commit/ac878ebbcb2755faedfb4c635c4dca19be8f4913))

<a name="2.1.0"></a>
## [2.1.0](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v2.1.0) (2026-03-11)

### Features

* **MQTT:** Publish tx info as a json object at the slice and radio level ([269508e](https://www.github.com/jeffu231/FlexRadioServices/commit/269508e51672618bccc74081a6b076967641a076))

### Bug Fixes

* Include client id on radio tx info ([2589aa1](https://www.github.com/jeffu231/FlexRadioServices/commit/2589aa1d12d72f442eb80c6afaf9bbee1ce61823))
* **Hygiene:** Code clean up to simplify logic ([b01a536](https://www.github.com/jeffu231/FlexRadioServices/commit/b01a5364b87271362e19f9492547ae9196d4ce6a))

### Documentation

* **README:** Update the readme to refer to the Wiki ([b7af70d](https://www.github.com/jeffu231/FlexRadioServices/commit/b7af70db9d061a1839e73cc9ec4643f79990c423))
* **README:** Update topic information docs ([1d6cc9e](https://www.github.com/jeffu231/FlexRadioServices/commit/1d6cc9ef0eaf51b691aa534c26ae61fd5cf0615f))

<a name="2.0.0"></a>
## [2.0.0](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v2.0.0) (2026-03-09)

### Features

* **Settings:** Add Configuration API ([16df08f](https://www.github.com/jeffu231/FlexRadioServices/commit/16df08fed5276b9d1bb5bf1259e59a4d5cd7e20c))

### Bug Fixes

* **Docker:** Update docker dev file documentation ([92af8e3](https://www.github.com/jeffu231/FlexRadioServices/commit/92af8e33e260277b23cf00143cb3b986e06f1162))
* **Docker:** Update dockerfile to remove redundant labels ([883e0de](https://www.github.com/jeffu231/FlexRadioServices/commit/883e0de18168cb3ebe97fee3c6718b12b89ea14c))
* **Docs:** Fix service name in Swagger UI ([ddbb4a5](https://www.github.com/jeffu231/FlexRadioServices/commit/ddbb4a559e8ba3e1801930a171c285c898ccd3ef))
* **Hygiene:** Clean up unused usings ([d921ee1](https://www.github.com/jeffu231/FlexRadioServices/commit/d921ee133c9f6b972d6a1685e67c4717b12df350))
* **Logging:** Improve error logging in the Radio API ([988e3e2](https://www.github.com/jeffu231/FlexRadioServices/commit/988e3e224656af9131ee947e7375d84b3111fbb1))

### Documentation

* **API:** Improve API Documentation for Swagger ([e3b1013](https://www.github.com/jeffu231/FlexRadioServices/commit/e3b1013e4f606810c2c243babfe440bf204b1098))

### Breaking Changes

* **Settings:** Add Configuration API ([16df08f](https://www.github.com/jeffu231/FlexRadioServices/commit/16df08fed5276b9d1bb5bf1259e59a4d5cd7e20c))

<a name="1.2.1"></a>
## [1.2.1](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v1.2.1) (2026-03-06)

### Bug Fixes

* **Build:** Ensure Example folder is ommited from build ([d35ef64](https://www.github.com/jeffu231/FlexRadioServices/commit/d35ef64cfd3f1a74c5fcbd342ede06b996951841))
* **Docs:** Update api docs ([8dd2358](https://www.github.com/jeffu231/FlexRadioServices/commit/8dd23586dec191096b2de12bd8a9e4338fbf964d))

<a name="1.2.0"></a>
## [1.2.0](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v1.2.0) (2026-03-06)

### Features

* **FlexLib:** Update to v4.1.5 of Flexlib ([4e4d3d6](https://www.github.com/jeffu231/FlexRadioServices/commit/4e4d3d628db41e5cbc2ec1b318e2ef384a2ee6b4))

<a name="1.1.1"></a>
## [1.1.1](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v1.1.1) (2026-03-06)

### Bug Fixes

* **api:** Isolate using the Flexlib objects in api endpoint ([692fc2b](https://www.github.com/jeffu231/FlexRadioServices/commit/692fc2b5fbc25f9c6ecba600f0f9d408f8cebdb8))

<a name="1.1.0"></a>
## [1.1.0](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v1.1.0) (2026-03-05)

### Features

* **Deps:** Upgrade api versoning to replace deprecated libraries ([cf9d136](https://www.github.com/jeffu231/FlexRadioServices/commit/cf9d13680c4587ba484a2db4e2941301382cd234))

### Bug Fixes

* Check resolved service for null to resolve warning ([9501783](https://www.github.com/jeffu231/FlexRadioServices/commit/950178328fbcd0fccf874225513035ee9abdeb13))
* **Deps:** Bump Config and Swashbuckle deps to latest ([b423ed3](https://www.github.com/jeffu231/FlexRadioServices/commit/b423ed3fac8a4144c660e1923f491bbfeac158cf))
* **Deps:** Bump MQTT to latest v4 versions ([ac61f2b](https://www.github.com/jeffu231/FlexRadioServices/commit/ac61f2bb268024e1f3667f4d2bbf3523354d47d6))

### Continuous Integration

* **Build:** Update build file to latest deps ([2bf2fa8](https://www.github.com/jeffu231/FlexRadioServices/commit/2bf2fa8d6bba06a3460d6446487003cdd97a9816))
* **Container:** Update container image description ([0edc95b](https://www.github.com/jeffu231/FlexRadioServices/commit/0edc95b526e925faecdc9d65f5a61a51cff2b0fe))

<a name="1.0.16"></a>
## [1.0.16](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v1.0.16) (2026-01-19)

### Bug Fixes

* add fetch depth to github workflow ([42cfdea](https://www.github.com/jeffu231/FlexRadioServices/commit/42cfdead471b8276a28fdf4acb61680c46c2fe55))
* formatting indentation in appsettings.user.json example ([0f177e1](https://www.github.com/jeffu231/FlexRadioServices/commit/0f177e1e5c544bd881cb5cf8839b460c15538e27))
* update logging format to json ([877fb14](https://www.github.com/jeffu231/FlexRadioServices/commit/877fb14ef57ab3e89dd916dc35b5599d1f569212))

### Continuous Integration

* Add log and build keywords to commit linter ([8654df7](https://www.github.com/jeffu231/FlexRadioServices/commit/8654df7daeadfff4898a43a2e9e2cb9f79605b5e))

<a name="1.0.15"></a>
## [1.0.15](https://www.github.com/jeffu231/FlexRadioServices/releases/tag/v1.0.15) (2025-06-19)

### Continuous Integration

* Add versionize and commit hooks ([fdc0933](https://www.github.com/jeffu231/FlexRadioServices/commit/fdc09335bcaff8bb8ab057c16b8102ce1462bab2))

