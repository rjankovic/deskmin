-- phpMyAdmin SQL Dump
-- version 3.3.2deb1ubuntu1
-- http://www.phpmyadmin.net
--
-- Hostiteľ: localhost
-- Vygenerované:: 18.Okt, 2012 - 17:37
-- Verzia serveru: 5.1.63
-- Verzia PHP: 5.3.2-1ubuntu4.18

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Databáza: `deskmin`
--

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `access_rights`
--

CREATE TABLE IF NOT EXISTS `access_rights` (
  `id_user` int(11) NOT NULL,
  `id_project` int(11) NOT NULL,
  `access` int(11) NOT NULL,
  PRIMARY KEY (`id_user`,`id_project`),
  KEY `id_project` (`id_project`),
  KEY `id_user` (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `fields`
--

CREATE TABLE IF NOT EXISTS `fields` (
  `id_field` int(11) NOT NULL AUTO_INCREMENT,
  `table_column` varchar(50) NOT NULL,
  `id_panel` int(11) NOT NULL,
  `id_type` int(11) NOT NULL,
  PRIMARY KEY (`id_field`),
  KEY `id_panel` (`id_panel`),
  KEY `id_type` (`id_type`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=4374 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `fields_meta`
--

CREATE TABLE IF NOT EXISTS `fields_meta` (
  `id_field` int(11) NOT NULL,
  `name` char(30) NOT NULL,
  `val` text NOT NULL,
  `concerns` enum('view','validation','controls') NOT NULL,
  PRIMARY KEY (`id_field`,`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `field_types`
--

CREATE TABLE IF NOT EXISTS `field_types` (
  `id_type` int(11) NOT NULL AUTO_INCREMENT,
  `type_name` varchar(50) NOT NULL,
  PRIMARY KEY (`id_type`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=17 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `log_db`
--

CREATE TABLE IF NOT EXISTS `log_db` (
  `id_log` bigint(20) NOT NULL AUTO_INCREMENT,
  `db_user` varchar(100) NOT NULL,
  `query` int(11) NOT NULL,
  `total_time` int(11) NOT NULL,
  `count` int(11) NOT NULL,
  `max_time` int(11) NOT NULL,
  `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_log`),
  KEY `user` (`db_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `log_users`
--

CREATE TABLE IF NOT EXISTS `log_users` (
  `id_log` bigint(20) NOT NULL AUTO_INCREMENT,
  `id_user` int(11) NOT NULL,
  `id_panel` int(11) NOT NULL,
  `action` varchar(50) NOT NULL,
  `param` tinyblob NOT NULL,
  `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `pid` int(11) NOT NULL,
  PRIMARY KEY (`id_log`),
  KEY `id_user` (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `panels`
--

CREATE TABLE IF NOT EXISTS `panels` (
  `id_panel` int(11) NOT NULL AUTO_INCREMENT,
  `table_name` varchar(50) DEFAULT NULL,
  `id_project` int(11) NOT NULL,
  `id_type` int(11) NOT NULL,
  `id_parent` int(11) DEFAULT NULL,
  `pk_column_names` varchar(255) NOT NULL,
  `id_holder` int(11) DEFAULT NULL,
  PRIMARY KEY (`id_panel`),
  UNIQUE KEY `table_name` (`table_name`,`id_type`),
  KEY `id_project` (`id_project`),
  KEY `id_holder` (`id_holder`),
  KEY `id_type` (`id_type`),
  KEY `id_parent` (`id_parent`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=908 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `panels_meta`
--

CREATE TABLE IF NOT EXISTS `panels_meta` (
  `id_panel` int(11) NOT NULL,
  `name` char(30) NOT NULL,
  `val` text NOT NULL,
  `concerns` enum('view','validation','controls') NOT NULL,
  PRIMARY KEY (`id_panel`,`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `panel_types`
--

CREATE TABLE IF NOT EXISTS `panel_types` (
  `id_type` int(11) NOT NULL AUTO_INCREMENT,
  `type_name` varchar(50) NOT NULL,
  PRIMARY KEY (`id_type`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=8 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `projects`
--

CREATE TABLE IF NOT EXISTS `projects` (
  `id_project` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `connstring_web` varchar(255) NOT NULL,
  `connstring_information_schema` varchar(255) NOT NULL,
  `server_type` varchar(10) NOT NULL,
  `server_version` varchar(10) NOT NULL,
  `last_modified` datetime NOT NULL,
  PRIMARY KEY (`id_project`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=3 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `requests`
--

CREATE TABLE IF NOT EXISTS `requests` (
  `id_request` int(11) NOT NULL AUTO_INCREMENT,
  `id_project` int(11) DEFAULT NULL,
  `action` varchar(50) NOT NULL,
  `when` datetime NOT NULL,
  `repeat` datetime DEFAULT NULL,
  PRIMARY KEY (`id_request`),
  KEY `id_project` (`id_project`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `users`
--

CREATE TABLE IF NOT EXISTS `users` (
  `id_user` int(11) NOT NULL AUTO_INCREMENT,
  `login` varchar(50) NOT NULL,
  `password` varchar(40) NOT NULL,
  `name` varchar(100) NOT NULL,
  `last_activity` datetime NOT NULL,
  PRIMARY KEY (`id_user`),
  KEY `login` (`login`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

--
-- Obmedzenie pre exportované tabuľky
--

--
-- Obmedzenie pre tabuľku `access_rights`
--
ALTER TABLE `access_rights`
  ADD CONSTRAINT `access_rights_ibfk_2` FOREIGN KEY (`id_project`) REFERENCES `projects` (`id_project`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `access_rights_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `users` (`id_user`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `fields`
--
ALTER TABLE `fields`
  ADD CONSTRAINT `fields_ibfk_1` FOREIGN KEY (`id_panel`) REFERENCES `panels` (`id_panel`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fields_ibfk_2` FOREIGN KEY (`id_type`) REFERENCES `field_types` (`id_type`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `fields_meta`
--
ALTER TABLE `fields_meta`
  ADD CONSTRAINT `fields_meta_ibfk_1` FOREIGN KEY (`id_field`) REFERENCES `fields` (`id_field`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `log_users`
--
ALTER TABLE `log_users`
  ADD CONSTRAINT `log_users_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `users` (`id_user`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `panels`
--
ALTER TABLE `panels`
  ADD CONSTRAINT `panels_ibfk_1` FOREIGN KEY (`id_project`) REFERENCES `projects` (`id_project`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `panels_ibfk_2` FOREIGN KEY (`id_type`) REFERENCES `panel_types` (`id_type`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `panels_ibfk_3` FOREIGN KEY (`id_parent`) REFERENCES `panels` (`id_panel`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `panels_ibfk_4` FOREIGN KEY (`id_holder`) REFERENCES `fields` (`id_field`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `panels_meta`
--
ALTER TABLE `panels_meta`
  ADD CONSTRAINT `panels_meta_ibfk_1` FOREIGN KEY (`id_panel`) REFERENCES `panels` (`id_panel`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `requests`
--
ALTER TABLE `requests`
  ADD CONSTRAINT `requests_ibfk_1` FOREIGN KEY (`id_project`) REFERENCES `projects` (`id_project`) ON DELETE CASCADE ON UPDATE CASCADE;
