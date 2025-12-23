# Politique de confidentialité

## 1. Introduction

La présente politique de confidentialité décrit comment l'application "Impression etiquette" (développée par Rémi DEHER) collecte, utilise et protège vos données. Nous nous engageons à respecter la confidentialité de vos informations.

## 2. Collecte et Utilisation des Données

L'application "Impression etiquette" est conçue pour fonctionner de manière autonome.

Données Personnelles : L'application ne collecte, ne stocke, ni ne transmet aucune information personnellement identifiable (nom, adresse, email, localisation, etc.).

Données Métier : Les informations saisies dans l'application (noms de produits, codes-barres EAN/QR, dates de scan) sont traitées uniquement à des fins de génération d'étiquettes et d'historique de production.

## 3. Stockage des Données
Mode Local (Par défaut) : Toutes les données sont stockées localement sur votre appareil dans un fichier de base de données sécurisé (SQLite). Le développeur n'a aucun accès à ces données.

Mode Serveur (Optionnel) : Si vous choisissez de configurer une base de données externe (MariaDB/MySQL) pour la synchronisation, les données transitent directement de votre appareil vers votre serveur. Ces données ne transitent jamais par des serveurs tiers appartenant au développeur.

## 4. Autorisations et Permissions
L'application requiert certaines permissions Windows pour fonctionner correctement :

runFullTrust (Accès complet) : Cette permission est strictement nécessaire pour :

Communiquer avec les pilotes d'imprimantes installés sur votre machine (via System.Drawing.Printing).

Créer et gérer le fichier de base de données local sur votre disque dur.

Réseau / Internet : L'accès au réseau est utilisé uniquement si vous activez la synchronisation avec votre propre base de données serveur ou pour vérifier les mises à jour de l'application.

## 5. Partage avec des Tiers
Nous ne vendons, n'échangeons, ni ne transférons vos données à des tiers. L'application ne contient aucun outil publicitaire ni aucun traceur d'analyse comportementale (type Google Analytics).

## 6. Vos Droits
Puisque toutes les données sont stockées sur votre appareil ou votre propre serveur, vous conservez le contrôle total de vos informations. Vous pouvez supprimer l'historique ou désinstaller l'application à tout moment pour effacer les données locales.

## 7. Contact
Pour toute question concernant cette politique de confidentialité, vous pouvez contacter le développeur via la page du projet : https://github.com/remi-deher/qr-code-generator
