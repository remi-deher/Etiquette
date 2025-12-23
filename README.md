# Impression d'étiquette

<img width="1283" height="746" alt="image" src="https://github.com/user-attachments/assets/c63a7142-cec5-4a34-90ee-ed17eda855a1" />

<img width="1283" height="746" alt="image" src="https://github.com/user-attachments/assets/6c740299-c973-4038-914d-77d224aea13c" />


## 📖 À propos

Application pour imprimer des étiquettes sur POS

## ✨ Fonctionnalités principales

* Génération et impression de QR Code
* Historique des codes générés
* A VENIR : Application pour Android pour envoyer en file d'attente des codes (pour terminal d'inventaire)
* 🎨 **Interface moderne :** Conçue pour Windows 10 et 11.
* Compatible avec les bases de donnée MySQL
* Fallback sur SQLite en cas de deconnexion avec le serveur et gestions des conflits

## 📥 Comment installer l'application

L'application est fournie au format `.msix`.

1.  Rendez-vous sur la page **[Releases](../../releases)** de ce dépôt.
2.  Téléchargez le dernier fichier portant l'extension `.msix`.

### ⚠️ Important : Première installation (Certificat)

Windows peut demander une vérification manuelle du certificat lors de la première installation si le certificat n'a pas été propagé.

**Si vous obtenez une erreur à l'ouverture, suivez ces étapes (à faire une seule fois) :**

1.  Faites un **clic-droit** sur le fichier `.msix` téléchargé et choisissez **Propriétés**.
2.  Allez dans l'onglet **Signatures numériques**, sélectionnez la signature dans la liste et cliquez sur **Détails**.
3.  Cliquez sur **Afficher le certificat** puis sur **Installer un certificat**.
4.  Sélectionnez **Ordinateur local** (Local Machine) et faites Suivant.
5.  Cochez **"Placer tous les certificats dans le magasin suivant"**.
6.  Cliquez sur **Parcourir...** et sélectionnez **"Autorités de certification racines de confiance"** (Trusted Root Certification Authorities).
7.  Validez par **OK**, puis **Suivant** et **Terminer**.

Une fois ceci fait, vous pouvez double-cliquer sur le fichier `.msix` pour l'installer normalement ! 🎉

## 🛠 Technologies utilisées

* **Langage :** C# / .NET
* **Framework :** WinUI 3
* **IDE :** Visual Studio 2026

## 🤝 Contribuer

N'hésitez pas à ouvrir une "Issue" si vous trouvez un bug ou si vous avez une idée d'amélioration.

## 📄 Licence

Distribué sous la licence MIT. Voir le fichier `LICENSE` pour plus d'informations.
