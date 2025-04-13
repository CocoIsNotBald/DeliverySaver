# DeliverySaver Changelog

All the changes, fixes and improvements made to the mod.

## DeliverySaver 1.0.2
### Added/Change
- Better error handling
- Template is now automatically open if you left it open when you closed the delivery app
- Template automatically open when you enter a valid seed or a valid template name
- Added a button to close the template input box or seed input box
- Added a button to validate the template input box or seed input box
### Fixed
- Fixed major issue about save by reworking the way template are handle
- Fixed counterintuitive close behaviour for inputs ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/e0b14d39bad33034aa66175efe8aa172fa084f9f))
- Close stream after the assets bundle are fully load ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/e0b14d39bad33034aa66175efe8aa172fa084f9f))
### Know major issues
- When changing save. Mod are trying to instantiate unity object but failed and this provoke looping and reinstantiate every object with will cause memory issues and potential game crash. I've tempered the issues with better error handling

## DeliverySaver 1.0.1
### Fixed
- Fixed thunderstore app not loading the dll

## DeliverySaver 1.0.0 (pre-release)
### Added/Change
- Changed the way assets manager assetsbundle ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/4d6425a6837e4357637f9c54682245e848a5fca4))
- Added a way to load file from assets ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/4d6425a6837e4357637f9c54682245e848a5fca4))
- Added file to src folder instead of root folder ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/85bcd7368dd910e8803218ceb4f39d50b13d6514))
- Added Equatability for Data type ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/cf15c9b534445ba02126161dd6973170368cddff))
- Ignored assets folder ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/cf15c9b534445ba02126161dd6973170368cddff))
- Added notification ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/7ab628bcebf6aaa8083fa07c9bda279ff6a58216))
- Added seeding ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/7ab628bcebf6aaa8083fa07c9bda279ff6a58216))
- Added per save template loading ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/86c2eff287fd7f123e9c0735d5256060e8e01f61))
- Added resources loading with il2cpp ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/86c2eff287fd7f123e9c0735d5256060e8e01f61))
- Added close button to entry ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/79c38f65290c476f1c0b7a3a91065c747e1887ea))
- Added delivery order template ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/db30227e34a2d40d451cb131068f27f6aed7faa3))
### Fixed
- Fixed entry name not updating in json ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/556d781a40b49975d9389e284ac0700094c0b3e3))
- Fixed unordered dictionary from json when loading template ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/556d781a40b49975d9389e284ac0700094c0b3e3))
- Fixed stack limit calculation problem ([Commit](https://github.com/CocoIsNotBald/DeliverySaver/commit/4d6425a6837e4357637f9c54682245e848a5fca4))
### Know issues
- Notification animation can is a bit buggy. Sometimes animation is flicking when nearing the end