BuildTestConsole Changelog
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

