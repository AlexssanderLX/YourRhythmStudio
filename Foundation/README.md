# Foundation

Bibliotecas reutilizaveis para acesso SaaS, links seguros com QR, frete e assistentes conversacionais.

## Objetivo

Esta solucao foi criada para servir como uma base generica e reaproveitavel em varios projetos, sem depender do dominio do ZeroPaper.

## Modulos

- `Foundation.Core`: resultados, erros, relogio, geracao de codigos e utilitarios comuns.
- `Foundation.Access`: base de acesso SaaS com administradores de plataforma, tenants, planos, cadastro com aprovacao, login por senha, login por codigo, sessao e seguranca de senha.
- `Foundation.SecureLinks`: links publicos seguros com expiracao opcional, limite de uso e geracao de QR.
- `Foundation.Freight`: cotacao de frete com taxa base, valor por km, cache e providers trocaveis.
- `Foundation.Assistant`: IA e canal de mensagens no mesmo projeto, com modulos internos separados.

## Samples

Os projetos em `samples/` mostram o uso basico de cada modulo usando implementacoes em memoria.

## Documentacao

- `docs/architecture.md`: visao estrutural da solucao
- `docs/module-scope.md`: responsabilidade e limite de cada modulo
- `docs/access-saas.md`: o que o Foundation.Access cobre como base SaaS

## Principios

- contratos claros antes de integracoes externas
- nomes neutros para funcionar em varios dominos
- implementacoes em memoria para testes e prototipos
- providers trocaveis para escalar quando o projeto exigir
