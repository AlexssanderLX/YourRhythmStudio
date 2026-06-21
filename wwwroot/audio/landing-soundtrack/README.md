# Trilha sonora da landing page

Esta pasta guarda as faixas de áudio usadas pelo **player flutuante** da landing page
(o botão "🎧 Ativar trilha sonora" que aparece no canto inferior direito após rolar a página).

## Como adicionar músicas

1. Coloque os arquivos de áudio **nesta pasta** (`wwwroot/audio/landing-soundtrack/`).
2. Use nomes simples e previsíveis:
   - `track-1.mp3`
   - `track-2.mp3`
   - `track-3.mp3`
3. Se adicionar mais faixas (track-4, track-5, ...), inclua o caminho correspondente
   na lista `PLAYLIST` em [`wwwroot/js/soundtrack.js`](../../js/soundtrack.js).

A playlist no JavaScript já aponta para esta pasta:

```js
const PLAYLIST = [
  "/audio/landing-soundtrack/track-1.mp3",
  "/audio/landing-soundtrack/track-2.mp3",
  "/audio/landing-soundtrack/track-3.mp3"
];
```

## Comportamento quando não há arquivos

- O player **não quebra** se os arquivos não existirem.
- Antes de aparecer, ele verifica (via requisição `HEAD`) quais faixas realmente existem.
- Se **nenhuma** faixa for encontrada, o player simplesmente **não aparece**
  (sem erro no console — apenas um `console.info` discreto).
- Assim que você adicionar pelo menos um arquivo válido, o player volta a aparecer.

## Formatos recomendados

- `.mp3` (melhor compatibilidade entre navegadores). `.ogg` / `.wav` também funcionam
  se você adicionar os caminhos na playlist.
- Volume inicial do player: **20%** (definido no `soundtrack.js`).

## Versionamento

As faixas **não são versionadas no Git** (veja o `.gitignore` desta pasta) — elas devem ser
adicionadas manualmente no servidor/deploy. Apenas a estrutura (este README + .gitignore)
fica no repositório.
