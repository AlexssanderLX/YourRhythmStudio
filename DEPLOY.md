# Deploy YourRhythm Studio

Este documento registra o deploy de producao do YourRhythm Studio na VPS `yourrhythm`.

Estado atual: aplicacao publicada, systemd ativo, Nginx ativo, HTTPS emitido com Certbot e migrations aplicadas. Nao registrar senhas, tokens, e-mail de Certbot, connection strings reais ou chaves privadas neste arquivo.

## Servidor

- Provedor: Vultr
- IP: `140.82.26.53`
- Hostname: `yourrhythm`
- SO: Ubuntu 24.04 LTS
- Dominios:
  - `yourrhythmstudio.com.br`
  - `www.yourrhythmstudio.com.br`
- Stack: ASP.NET Core MVC, EF Core, MySQL, Nginx, Kestrel e systemd

## Versoes

- Projeto: `net10.0`
- Runtime na VPS: ASP.NET Core Runtime `10.0.9`
- MySQL: `8.0.46`
- Nginx: `1.24.0`
- Certbot: `2.9.0`

## SSH

Atalhos locais:

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

Recuperacao:

```bash
ssh yourrhythm-vps-root
```

SSH efetivo:

- `PasswordAuthentication no`
- `PubkeyAuthentication yes`
- `PermitRootLogin prohibit-password`

## Usuarios

- `alexssander`: usuario administrativo com sudo.
- `yourrhythm`: usuario de servico sem login interativo.
- `root`: preservado apenas para recuperacao por chave.

## Firewall e portas

UFW:

- entrada negada por padrao;
- saida permitida;
- liberado: SSH, HTTP e HTTPS.

Portas esperadas:

- publico: `22`, `80`, `443`
- local apenas: `127.0.0.1:5000` para Kestrel
- local apenas: `127.0.0.1:3306` para MySQL

Verificacao:

```bash
sudo ufw status verbose
sudo ss -tulpn
```

## Banco

Banco de producao:

```text
yourrhythm_prod
```

Usuario da aplicacao:

```text
yourrhythm_app@localhost
```

A senha real foi gerada diretamente na VPS e fica apenas no arquivo protegido de ambiente. Nao registrar essa senha em Git, chat ou docs.

Migrations aplicadas:

- tabela `__EFMigrationsHistory`: `15` entradas
- tabelas no banco `yourrhythm_prod`: `25`
- ultima migration: `20260713033215_MissionsAndRepertoireEvolution_Auto`

Comando de verificacao:

```bash
sudo mysql --protocol=socket -N -e "SELECT COUNT(*) FROM yourrhythm_prod.__EFMigrationsHistory;"
```

## Estrutura

Releases:

```text
/var/www/yourrhythm/releases/
```

Release ativa:

```text
/var/www/yourrhythm/releases/2026-07-13-0836
```

Symlink ativo:

```text
/var/www/yourrhythm/current -> /var/www/yourrhythm/releases/2026-07-13-0836
```

Persistencia fora da release:

```text
/var/www/yourrhythm/shared/uploads/wwwroot
/var/www/yourrhythm/shared/uploads/storage
/var/www/yourrhythm/shared/logs
/var/www/yourrhythm/shared/backups
```

Symlinks por release:

```text
/var/www/yourrhythm/current/wwwroot/uploads -> /var/www/yourrhythm/shared/uploads/wwwroot
/var/www/yourrhythm/current/storage/uploads -> /var/www/yourrhythm/shared/uploads/storage
```

## Ambiente

Arquivo protegido:

```text
/etc/yourrhythm/yourrhythm.env
```

Permissoes:

```text
root:yourrhythm
640
```

Variaveis esperadas:

```text
ASPNETCORE_ENVIRONMENT
ASPNETCORE_URLS
ASPNETCORE_FORWARDEDHEADERS_ENABLED
ASPNETCORE_HTTPS_PORT
ConnectionStrings__DefaultConnection
Email__Smtp__Host
Email__Smtp__Port
Email__Smtp__UseSsl
Email__Smtp__Username
Email__Smtp__Password
Email__Smtp__SenderEmail
Email__Smtp__SenderName
Email__AdminNotificationRecipient
Logging__LogLevel__Microsoft
```

## Root

A conta Root inicial e criada automaticamente na primeira subida. A credencial inicial foi rotacionada apos o bootstrap.

Arquivo protegido com a credencial atual:

```text
/etc/yourrhythm/root-credentials.txt
```

Permissao:

```text
root:root
600
```

Depois do primeiro login, alterar a senha em `/Root/Settings` e remover esse arquivo.

## Publish

Comandos usados localmente:

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
dotnet publish ./YourRhythmStudio.csproj -c Release -r linux-x64 --self-contained false -o ./artifacts/publish-yourrhythm
```

Antes do envio, foram removidos do publish:

- `appsettings.Development.json`
- pastas de teste/build copiadas acidentalmente
- uploads locais

Artefatos enviados:

```text
/tmp/yourrhythm-2026-07-13-0836.tar.gz
/tmp/yourrhythm-migrations-2026-07-13-0836.sql
```

## Systemd

Arquivo:

```text
/etc/systemd/system/yourrhythm.service
```

Conteudo relevante:

```ini
[Unit]
Description=YourRhythm Studio ASP.NET Core Application
After=network.target mysql.service
Wants=mysql.service

[Service]
Type=simple
User=yourrhythm
Group=yourrhythm
WorkingDirectory=/var/www/yourrhythm/current
EnvironmentFile=/etc/yourrhythm/yourrhythm.env
ExecStart=/usr/bin/dotnet /var/www/yourrhythm/current/YourRhythmStudio.dll
Restart=on-failure
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=yourrhythm
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=full
ReadWritePaths=/var/www/yourrhythm /etc/yourrhythm

[Install]
WantedBy=multi-user.target
```

Comandos:

```bash
sudo systemd-analyze verify /etc/systemd/system/yourrhythm.service
sudo systemctl daemon-reload
sudo systemctl enable yourrhythm
sudo systemctl restart yourrhythm
sudo systemctl status yourrhythm
sudo journalctl -u yourrhythm -n 100 --no-pager
```

Status esperado:

```text
active
enabled
```

## Nginx

Arquivo:

```text
/etc/nginx/sites-available/yourrhythm.conf
/etc/nginx/sites-enabled/yourrhythm.conf
```

O site `default` foi desabilitado em `sites-enabled`.

Conteudo relevante:

```nginx
server {
    server_name yourrhythmstudio.com.br www.yourrhythmstudio.com.br;

    access_log /var/log/nginx/yourrhythm.access.log;
    error_log /var/log/nginx/yourrhythm.error.log;

    client_max_body_size 100M;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_read_timeout 300s;
        proxy_send_timeout 300s;
        proxy_connect_timeout 60s;
    }

    listen [::]:443 ssl ipv6only=on;
    listen 443 ssl;
    ssl_certificate /etc/letsencrypt/live/yourrhythmstudio.com.br/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourrhythmstudio.com.br/privkey.pem;
    include /etc/letsencrypt/options-ssl-nginx.conf;
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;
}
```

O bloco HTTP foi gerenciado pelo Certbot e redireciona para HTTPS.

Verificacao:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

## SSL

Certificado emitido para:

- `yourrhythmstudio.com.br`
- `www.yourrhythmstudio.com.br`

Arquivos:

```text
/etc/letsencrypt/live/yourrhythmstudio.com.br/fullchain.pem
/etc/letsencrypt/live/yourrhythmstudio.com.br/privkey.pem
```

Renovacao:

```bash
sudo certbot renew --dry-run
systemctl list-timers --all | grep -i certbot
```

Resultado do dry-run:

```text
success
```

## Testes Pos-Deploy

Comandos executados:

```bash
curl -I http://yourrhythmstudio.com.br/
curl -I http://www.yourrhythmstudio.com.br/
curl -I https://yourrhythmstudio.com.br/
curl -I https://www.yourrhythmstudio.com.br/
curl -L https://yourrhythmstudio.com.br/Auth/Login
curl --resolve yourrhythmstudio.com.br:443:140.82.26.53 https://yourrhythmstudio.com.br/
curl --resolve www.yourrhythmstudio.com.br:443:140.82.26.53 https://www.yourrhythmstudio.com.br/
```

Resultados:

- HTTP raiz: `301` para HTTPS
- HTTP www: `301` para HTTPS
- HTTPS raiz: `200`
- HTTPS www: `200`
- `/Auth/Login`: carrega HTML de login via `GET`
- CSS principal carregando
- Origin direto com SNI: `200`
- Kestrel escuta apenas em `127.0.0.1:5000`
- MySQL escuta apenas em `127.0.0.1:3306`

## Rollback

1. Ver releases disponiveis:

```bash
ls -la /var/www/yourrhythm/releases
readlink -f /var/www/yourrhythm/current
```

2. Apontar para a release anterior:

```bash
sudo ln -sfn /var/www/yourrhythm/releases/NOME_DA_RELEASE_ANTERIOR /var/www/yourrhythm/current
sudo chown -h yourrhythm:yourrhythm /var/www/yourrhythm/current
```

3. Reiniciar e validar:

```bash
sudo systemctl restart yourrhythm
sudo systemctl status yourrhythm
curl -I http://127.0.0.1:5000/
sudo nginx -t
sudo systemctl reload nginx
```

Nao remover a release anterior ate validar o rollback e receber aprovacao.

## Diagnostico Rapido

```bash
sudo systemctl status yourrhythm
sudo journalctl -u yourrhythm -n 120 --no-pager
sudo systemctl status nginx
sudo nginx -t
sudo tail -n 100 /var/log/nginx/yourrhythm.error.log
sudo tail -n 100 /var/log/nginx/yourrhythm.access.log
sudo systemctl status mysql
sudo ufw status verbose
sudo ss -tulpn
df -hT /
free -h
```

## Cloudflare

Nao foram alteradas configuracoes de Cloudflare por este deploy.

Estado observado:

- DNS publico resolve para IPs da Cloudflare.
- HTTP/HTTPS via Cloudflare chega ao origin.
- Origin possui certificado Let’s Encrypt valido.

Pendencias para Alexssander na Cloudflare:

1. Conferir se SSL/TLS esta em modo adequado para origin com certificado valido, preferencialmente Full (Strict).
2. Conferir se os registros `A`/`CNAME` continuam apontando para a VPS/origin esperado.
3. Decidir politicas de cache; nao aplicar cache agressivo em paginas autenticadas.

## Observacoes

- O pacote `fwupd` ficou retido pelo Ubuntu em atualizacao anterior; sem impacto conhecido no app.
- O e-mail SMTP ainda precisa ser configurado no ambiente caso notificacoes reais sejam necessarias.
- A credencial Root deve ser trocada no primeiro acesso e o arquivo protegido deve ser removido depois.
