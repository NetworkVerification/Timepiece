IMAGE := timepiece
CONTAINER := tptest
NTRIALS := 1
TIMEOUT := 60
MINSIZE := 4
MAXSIZE := 40
LOGDIR := logs
RESULTDIR := results
ANGLERDIR := angler
# set to 1 if you want to force the repo to clone angler when INTERNET2.angler.json is present
FORCE_CLONE := 1
POLICIES := r lw v h ar alw av ah

.PHONY: all
all: plots

# build the docker image
.PHONY: image
image: Dockerfile INTERNET2.angler.json run_all.py
	docker build --rm -t $(IMAGE) .

# run the monolithic benchmark
$(RESULTDIR)/%-m.dat: | image
	docker run --name $(CONTAINER) $(IMAGE) python3.9 ./run_all.py -d /timepiece/publish -n $(NTRIALS) -t $(TIMEOUT) -k $(MINSIZE) $(MAXSIZE) --dat -- $(*F) -m
	docker cp $(CONTAINER):/timepiece/logs .
	docker cp $(CONTAINER):/timepiece/results .
	docker rm $(CONTAINER)

# run the modular benchmark
$(RESULTDIR)/%.dat: | image
	docker run --name $(CONTAINER) $(IMAGE) python3.9 ./run_all.py -d /timepiece/publish -n $(NTRIALS) -t $(TIMEOUT) -k $(MINSIZE) $(MAXSIZE) --dat -- $(*F)
	docker cp $(CONTAINER):/timepiece/logs .
	docker cp $(CONTAINER):/timepiece/results .
	docker rm $(CONTAINER)

# generate the plot from the benchmarks
# use the first prereq as \benchmod and the second as \benchmono
$(RESULTDIR)/%.pdf: $(RESULTDIR)/%.dat $(RESULTDIR)/%-m.dat
	pdflatex -jobname $(*F) -halt-on-error -output-directory results "\newcommand\timeout{$(TIMEOUT)}\newcommand\benchmod{$(word 1,$(^F))}\newcommand\benchmono{$(word 2,$(^F))}\input{plot.tex}"

bench:	$(addprefix $(RESULTDIR)/, $(POLICIES:=.dat)) $(addprefix $(RESULTDIR)/, $(POLICIES:=-m.dat))

plots:	$(addprefix $(RESULTDIR)/, $(POLICIES:=.pdf))

wan: $(LOGDIR)/internet2.txt $(LOGDIR)/internet2-m.txt

$(LOGDIR)/internet2.txt: INTERNET2.angler.json | $(LOGDIR) image
	docker run --rm --name $(CONTAINER) $(IMAGE) timeout $(TIMEOUT) dotnet /timepiece/publish/Timepiece.Angler.dll $< | tee $@

$(LOGDIR)/internet2-m.txt: INTERNET2.angler.json | $(LOGDIR) image
	docker run --rm --name $(CONTAINER) $(IMAGE) timeout $(TIMEOUT) dotnet /timepiece/publish/Timepiece.Angler.dll $< -m | tee $@

INTERNET2.angler.json:
	cd $(ANGLERDIR); sh ./internet2.sh; cd -
	cp $(ANGLERDIR)/INTERNET2.angler.json .

$(LOGDIR):
	mkdir -p $(LOGDIR)

.PHONY: clean
clean:
	  rm -rf $(RESULTDIR)
	  rm -rf $(LOGDIR)
