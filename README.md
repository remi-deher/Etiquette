# 🏷️ Etiquette

> Solution moderne d'impression d'étiquettes et de gestion de codes pour points de vente (POS).

[![Microsoft Store](https://img.shields.io/badge/Microsoft%20Store-Télécharger-blue?logo=microsoft&logoColor=white)](https://apps.microsoft.com/detail/9PDMT6H4VCZX)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?logo=windows)
![Framework](https://img.shields.io/badge/Framework-WinUI%203-purple?logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green)

<div align="center">
<img width="1906" height="746" alt="image" src="https://github.com/user-attachments/assets/571eae64-29d0-4c0e-9275-5cf5bbc2f4bd" />
</div>

## 📖 À propos

**Etiquettes** est une application conçue pour simplifier la génération et l'impression d'étiquettes en environnement commercial. Développée avec les dernières technologies Windows (WinUI 3), elle assure une fiabilité maximale grâce à sa gestion intelligente des bases de données.

## ✨ Fonctionnalités Clés

### 🖨️ Impression & Gestion
* **Génération de QR Codes & Code-barres** instantanée.
* **Historique complet** des codes générés et imprimés.
* **Files d'attente** : Gestion des impressions en attente.

### 🛡️ Fiabilité & Réseau
* **Mode Hybride (Offline First)** : Fonctionne principalement avec une base de données MySQL, mais bascule automatiquement sur une base locale **SQLite** en cas de coupure réseau. Les données sont resynchronisées au retour de la connexion.
* **Découverte Réseau (Auto-Discovery)** : Utilise le protocole UDP pour détecter automatiquement les instances serveur sur le réseau local sans configuration complexe d'IP.
* **Appairage Sécurisé** : Système d'échange de configuration chiffré pour connecter de nouveaux terminaux facilement.

### 🎨 Expérience Utilisateur
* Interface **Fluent Design** moderne (Windows 11).
* Compatible **Thème Sombre / Clair**.

## 🚀 Installation

### Via le Microsoft Store
Les mises à jour sont automatiques et l'installation est sécurisée.

* **Lien Web :** [https://apps.microsoft.com/detail/9PDMT6H4VCZX](https://apps.microsoft.com/detail/9PDMT6H4VCZX)
* **Lien Direct (Ouvrir le Store) :** `ms-windows-store://pdp/?productid=9PDMT6H4VCZX`


## 🛠️ Architecture & Technologies

Ce projet est construit sur des bases solides pour garantir maintenabilité et performance :

* **Langage :** C# / .NET 10
* **Interface :** WinUI 3 (Windows App SDK)
* **Architecture :** MVVM (Model-View-ViewModel)
* **Données :** Entity Framework Core (MySQL + SQLite)
* **Réseau :**
    * `UdpClient` pour la découverte de services (Broadcast).
    * `HttpListener` pour l'API locale de configuration.
    * Chiffrement asymétrique pour l'échange de clés.

## 🔮 Roadmap

* [ ] Application compagnon **Android** pour terminaux d'inventaire (envoi de codes vers la file d'attente).

## 🤝 Contribuer

Les contributions sont les bienvenues !
Si vous trouvez un bug ou souhaitez proposer une fonctionnalité, n'hésitez pas à ouvrir une **Issue**.

## 📄 Licence

Distribué sous la licence MIT. Voir le fichier `LICENSE` pour plus d'informations.
