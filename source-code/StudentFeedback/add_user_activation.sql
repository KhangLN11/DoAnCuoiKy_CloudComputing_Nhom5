START TRANSACTION;
ALTER TABLE `Users` ADD `IsActive` tinyint(1) NOT NULL DEFAULT TRUE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261017143009_AddUserActivationStatus', '9.0.0');

COMMIT;

