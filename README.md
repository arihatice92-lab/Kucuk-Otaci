# Küçük Otacı

## Ekip

- Meryem
- Hatice

## Proje Amacı

Küçük Otacı, Unity ve C# ile geliştirilen 3D bir macera oyunudur. Proje, aynı oyun içinde iki bağımsız yapay zeka bileşenini birleştirmeyi amaçlar: NavMesh ve Raycast kullanan otonom yardımcı karakter Fındık ile Ollama üzerinde yerel olarak çalışan büyük dil modeli destekli Basri Amca NPC'si.


## Oyun Senaryosu

Oyuncu, Hasat Şenliği için şifalı ot toplamak üzere ormana girer. Şiddetli yağmur ve sel nedeniyle yol su altında kalmıştır; ayrıca selin getirdiği büyük bir ağaç yolun üzerine devrilmiştir. Oyuncu bu fiziksel engelleri tek başına aşamaz.

Oyuncunun otonom yardımcısı Fındık, oyuncunun komutuyla NavMesh kullanarak vana koluna ulaşır. Raycast sensörleriyle çevresini algılar, yolu takip eder ve vana ile etkileşime geçerek suyun çekilmesini sağlar. Su çekildikten sonra Fındık devrilen ağacın kaldırılmasına yardımcı olur ve oyuncunun ormanda ilerleyebileceği yolu açar.

Oyuncu ve Fındık yol açıldıktan sonra yardıma ihtiyacı olan bir hayvanla karşılaşır. Oyuncu hayvana yardım etmeyi veya yoluna devam etmeyi seçer. Bu seçim Unity tarafında oyun durumu olarak kaydedilir.

Oyuncu daha sonra ormanda şifalı otları toplayarak köy kapısına ulaşır. Köyün bekçisi Basri Amca ile hazır seçenekler yerine serbest metin kullanarak konuşur. Basri Amca'nın Ollama üzerinde çalışan yerel LLM tarafından üretilen yapılandırılmış yanıtı, oyuncunun açıklamasını ve Unity'nin gönderdiği gerçek oyun durumunu değerlendirir. Oyuncunun hayvana yardım etmiş olması, şifalı ot toplaması ve konuşmasının ikna ediciliği kapının açılması kararını etkileyebilir. Yanıt sonucunda kapı açılır, Basri Amca ek soru sorar veya oyuncuyu geçici olarak reddeder.

## Yapay Zeka Bileşenleri

### Fındık - Otonom Companion

Fındık, oyuncunun yanında bekleyen otonom yardımcı karakterdir. Oyuncunun komutu üzerine NavMesh kullanarak vana koluna ulaşır ve suyun çekilmesini sağlar. Raycast sensörleriyle çevresindeki engelleri algılar. Fiziksel engeller kaldırıldıktan sonra oyuncunun ilerlemesine yardım eder.

Fındık'ın davranışı önceden belirlenmiş hareket adımlarına dayanmaz. Hedefe ulaşmak için NavMesh üzerinde yol bulur ve durum makinesi ile bekleme, komut alma, hedefe ilerleme, engel algılama, etkileşim ve oyuncuya dönme durumlarını yönetir.

### Basri Amca - Yerel LLM Destekli NPC

Basri Amca, köy kapısını koruyan şüpheci fakat iyi niyetli bir bekçidir. Oyuncu Basri Amca ile hazır cevap seçenekleri yerine serbest metin aracılığıyla konuşur.

Basri Amca'nın yanıtları, Ollama üzerinde yerel olarak çalışan büyük dil modeli tarafından üretilir. Modelin yanıtı `dialogue`, `decision` ve `mood` alanlarını içeren JSON formatındadır. Unity, JSON yanıtını doğrular ve yalnız geçerli kararları oyun durumuna uygular.

Oyuncunun topladığı otlar ve hayvana yardım edip etmediği gibi bilgiler Unity tarafından tutulur. Bu bilgiler LLM'ye bağlam olarak iletilir; LLM'nin oyun durumunu tahmin etmesine izin verilmez.

## Temel Oyun Akışı

1. Oyuncu ormana girer.
2. Sel nedeniyle su altında kalan ve devrilen ağaçla kapanan yola ulaşır.
3. Oyuncu Fındık'a komut verir.
4. Fındık NavMesh ve Raycast kullanarak vana koluna ulaşır.
5. Vana çalışır, su çekilir ve Fındık yol üzerindeki devrilen ağacın kaldırılmasına yardım eder.
6. Oyuncu ve Fındık, yardıma ihtiyacı olan hayvanla karşılaşır.
7. Oyuncunun hayvana yardım etme veya yoluna devam etme seçimi oyun durumuna kaydedilir.
8. Oyuncu şifalı otları toplar ve köy kapısına ulaşır.
9. Oyuncu Basri Amca ile serbest metin aracılığıyla konuşur.
10. Unity, oyuncu mesajını ve gerçek oyun durumunu Ollama'ya gönderir.
11. Ollama, `dialogue`, `decision` ve `mood` içeren JSON yanıtı üretir.
12. Unity geçerli yanıta göre kapıyı açar, ek konuşma başlatır veya geçici ret durumunu uygular.
13. Kapı açılırsa oyuncu Hasat Şenliği alanına ulaşır ve oyun tamamlanır.

## Kullanılan Teknolojiler

- Unity
- C#
- NavMesh
- Raycast
- Ollama
- Yerel LLM
- Git ve GitHub
- Markdown ve Mermaid