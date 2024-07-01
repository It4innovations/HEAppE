# Postup Migrace z Verze 4.2.1 na Verzi 4.2.2

Tento dokument popisuje kroky potřebné k migraci z verze 4.2.1 na verzi 4.2.2.

## Krok 1: Záloha Dat

Před provedením jakékoliv aktualizace je důležité zálohovat celou databázi. Zálohu celé databáze provedete pomocí příslušného nástroje pro správu vaší databáze.

Dále je potřeba exportovat data do formátu, který bude možné importovat do Hashicorp Vault. K tomu použijte skript `backupDataToCsv.sh`.

```bash
./backupDataToCsv.sh
```

Tento skript vytvoří export dat ve formátu CSV, který bude následně zpracován skriptem `importDataToVault.sh`.

## Krok 2: Migrace na Novou Verzi 

Po úspěšném zálohování dat můžete přistoupit k migraci HEAppE na verzi 4.2.2.

## Krok 3: Inicializace Vaultu

Před importem dat do vaultu je nutné provést inicializaci vaultu. K tomu použijte skript `vaultInitToAnsibleFile.sh`.

```bash
./vaultInitToAnsibleFile.sh
```

## Krok 4: Import Dat do Vaultu

Po úspěšné inicializaci vaultu můžete importovat data pomocí skriptu `importDataToVault.sh`.

```bash
./importDataToVault.sh
```

Tím je migrace dokončena. Doporučuje se zkontrolovat, zda byla všechna data správně importována a že aplikace funguje podle očekávání.