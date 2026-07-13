# Deploy YourRhythm Studio

Este documento registra a preparacao inicial da VPS de producao do YourRhythm Studio.

Estado atual: VPS preparada para receber a aplicacao ASP.NET Core, mas sem deploy da aplicacao, sem migration de producao, sem DNS/Cloudflare e sem certificado SSL emitido.

## Servidor

- Provedor: Vultr
- IP: `140.82.26.53`
- Hostname: `yourrhythm`
- SO: Ubuntu 24.04 LTS
- Dominio futuro: `yourrhythmstudio.com.br`
- Stack alvo: ASP.NET Core MVC, EF Core, MySQL, Nginx, Kestrel e systemd

## Acesso SSH

Atalhos locais criados em `~/.ssh/config`:

```sshconfig
Host yourrhythm-vps
  HostName 140.82.26.53
  User alexssander
  IdentityFile ~/.ssh/yourrhythm_vps
  IdentitiesOnly yes
  ServerAliveInterval 30

Host yourrhythm-vps-root
  HostName 140.82.26.53
  User root
  IdentityFile ~/.ssh/yourrhythm_vps
  IdentitiesOnly yes
  ServerAliveInterval 30
```

Uso normal:

```bash
ssh yourrhythm-vps
```

Acesso de recuperacao, preservado por enquanto:

```bash
ssh yourrhythm-vps-root
```

SSH foi configurado para chave publica:

- `PasswordAuthentication no`
- `PubkeyAuthentication yes`
- `PermitRootLogin prohibit-password`

Nao remover o acesso root por chave ate confirmar alguns ciclos reais de manutencao usando `alexssander`.

## Usuarios

- `alexssander`: usuario administrativo com sudo.
- `yourrhythm`: usuario de servico sem login interativo, usado pelo systemd.
- `root`: preservado somente para recuperacao por chave.

## Pacotes instalados

```bash
sudo apt-get update
sudo apt-get upgrade
sudo apt-get install ca-certificates curl gnupg lsb-release apt-transport-https ufw fail2ban mysql-server nginx certbot python3-certbot-nginx
```

Runtime .NET instalado:

```bash
curl -fsSL -o packages-microsoft-prod.deb https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install aspnetcore-runtime-10.0
```

O projeto usa `net10.0`; por isso o runtime instalado foi ASP.NET Core Runtime 10.

## Firewall

UFW esta ativo com entrada negada por padrao e as portas abaixo liberadas:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

Verificacao:

```bash
sudo ufw status verbose
```

## Fail2Ban

Arquivo criado:

```text
/etc/fail2ban/jail.d/yourrhythm-sshd.local
```

Conteudo:

```ini
[sshd]
enabled = true
port = ssh
filter = sshd
backend = systemd
maxretry = 5
findtime = 10m
bantime = 1h
```

Verificacao:

```bash
sudo fail2ban-client status sshd
```

## MySQL

MySQL foi instalado e mantido ouvindo localmente em `127.0.0.1`.

Endurecimento basico executado:

```sql
DELETE FROM mysql.user WHERE User='';
DELETE FROM mysql.user WHERE User='root' AND Host NOT IN ('localhost');
DROP DATABASE IF EXISTS test;
DELETE FROM mysql.db WHERE Db='test' OR Db='test\\_%';
FLUSH PRIVILEGES;
```

Ainda falta criar o banco e usuario da aplicacao quando o deploy for aprovado.

Exemplo para etapa futura, sem usar senha real aqui:

```sql
CREATE DATABASE yourrhythm_prod CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'yourrhythm_app'@'localhost' IDENTIFIED BY 'CHANGE_ME';
GRANT ALL PRIVILEGES ON yourrhythm_prod.* TO 'yourrhythm_app'@'localhost';
FLUSH PRIVILEGES;
```

## Estrutura da aplicacao

Diretorios criados:

```text
/var/www/yourrhythm
/var/www/yourrhythm/app
/var/www/yourrhythm/releases
/var/www/yourrhythm/shared
/var/www/yourrhythm/config
/var/www/yourrhythm/logs
/var/www/yourrhythm/backups
/var/www/yourrhythm/uploads
```

O dono e o grupo sao `yourrhythm`.

Arquivo de exemplo criado:

```text
/var/www/yourrhythm/config/yourrhythm.env.example
```

Ele contem placeholders e deve ser copiado futuramente para:

```text
/var/www/yourrhythm/config/yourrhythm.env
```

Nunca colar connection string real, senha SMTP, tokens ou segredos neste arquivo do repositorio.

## Systemd

Arquivo criado:

```text
/etc/systemd/system/yourrhythm.service
```

Status atual esperado:

- `disabled`
- `inactive`

Isso e intencional porque a aplicacao ainda nao foi publicada na VPS.

Comandos futuros:

```bash
sudo systemctl daemon-reload
sudo systemctl enable yourrhythm
sudo systemctl start yourrhythm
sudo systemctl status yourrhythm
journalctl -u yourrhythm -f
```

Rollback do servico:

```bash
sudo systemctl stop yourrhythm || true
sudo systemctl disable yourrhythm || true
sudo rm -f /etc/systemd/system/yourrhythm.service
sudo systemctl daemon-reload
```

## Nginx

Arquivo preparado:

```text
/etc/nginx/sites-available/yourrhythm.conf
```

Dominios configurados:

```text
yourrhythmstudio.com.br
www.yourrhythmstudio.com.br
```

Proxy preparado para Kestrel:

```text
http://127.0.0.1:5000
```

Importante:

- A configuracao foi criada em `sites-available`.
- Ela ainda nao foi habilitada em `sites-enabled`.
- Nenhum certificado SSL foi emitido.
- DNS/Cloudflare ainda nao foram alterados.

Quando for publicar:

```bash
sudo ln -s /etc/nginx/sites-available/yourrhythm.conf /etc/nginx/sites-enabled/yourrhythm.conf
sudo nginx -t
sudo systemctl reload nginx
```

Depois que DNS apontar corretamente:

```bash
sudo certbot --nginx -d yourrhythmstudio.com.br -d www.yourrhythmstudio.com.br
```

Rollback do Nginx:

```bash
sudo rm -f /etc/nginx/sites-enabled/yourrhythm.conf
sudo nginx -t
sudo systemctl reload nginx
```

## Verificacoes executadas

```bash
ssh yourrhythm-vps 'whoami && sudo -n whoami && hostname'
ssh yourrhythm-vps-root 'whoami && hostname'
sudo ufw status verbose
sudo fail2ban-client status sshd
sudo ss -tulpn
dotnet --info
mysql --version
nginx -v
certbot --version
sudo nginx -t
systemctl is-active ssh nginx mysql fail2ban
systemctl is-enabled yourrhythm.service
systemctl is-active yourrhythm.service
```

Resultado esperado no estado preparado:

- `ssh`: ativo
- `nginx`: ativo
- `mysql`: ativo
- `fail2ban`: ativo
- `yourrhythm.service`: desabilitado e inativo
- porta `22`: aberta
- porta `80`: aberta
- porta `443`: liberada no firewall, mas sem listener SSL ate emitir certificado
- MySQL ouvindo localmente em `127.0.0.1:3306`

## Proxima etapa pendente de Alexssander

Antes de publicar:

1. Apontar DNS/Cloudflare para `140.82.26.53`.
2. Definir senha/usuario do banco da aplicacao.
3. Criar `/var/www/yourrhythm/config/yourrhythm.env` com valores reais diretamente na VPS.
4. Publicar o app em `/var/www/yourrhythm/app`.
5. Aplicar migrations de producao conscientemente.
6. Habilitar `yourrhythm.service`.
7. Habilitar o site Nginx.
8. Emitir SSL com Certbot.

## O que nao foi feito

- Nao foi feito deploy da aplicacao.
- Nao foram executadas migrations.
- Nao foi criado banco de producao da aplicacao.
- Nao foi emitido certificado SSL.
- Nao foi alterado DNS/Cloudflare.
- Nao foi removido acesso root por chave.
- Nao foram gravados segredos no repositorio.
