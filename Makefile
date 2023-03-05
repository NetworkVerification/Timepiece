BENCHMARKS := Timepiece.Benchmarks
IMAGE := timepiece
CONTAINER := tptest
NTRIALS := 1
TIMEOUT := 30
MAXSIZE := 40
LOGDIR := logs
RESULTDIR := results
POLICIES := r lw v h ar alw av ah

.PHONY: all
all: plots

# build the docker image
image: Dockerfile
	docker build --rm -t $(IMAGE) .

# run the monolithic benchmark
$(LOGDIR)/%-m.txt: image run_all.py
	docker run --name $(CONTAINER) $(IMAGE) python3.9 ./run_all.py -d /timepiece/publish -n $(NTRIALS) -t $(TIMEOUT) -k 4 $(MAXSIZE) -- $(patsubst $(LOGDIR)/%-m.txt,% -m,$@)
	docker cp $(CONTAINER):/timepiece/logs .
	docker rm $(CONTAINER)

# run the modular benchmark
$(LOGDIR)/%.txt: image run_all.py
	docker run --name $(CONTAINER) $(IMAGE) python3.9 ./run_all.py -d /timepiece/publish -n $(NTRIALS) -t $(TIMEOUT) -k 4 $(MAXSIZE) -- $(patsubst $(LOGDIR)/%.txt,%,$@)
	docker cp $(CONTAINER):/timepiece/logs .
	docker rm $(CONTAINER)

# generate the plot from the benchmarks
$(RESULTDIR)/%plot.pdf: $(LOGDIR)/%.txt $(LOGDIR)/%-m.txt make_plots.sh
	bash make_plots.sh $(TIMEOUT) $(patsubst $(RESULTDIR)/%plot.pdf,%,$@)

bench:	$(addprefix $(LOGDIR)/, $(POLICIES:=.txt)) $(addprefix $(LOGDIR)/, $(POLICIES:=-m.txt))

plots:	$(addprefix $(RESULTDIR)/, $(POLICIES:=plot.pdf))

.PHONY: clean
clean:
	rm -rf $(RESULTDIR)
	rm -rf $(LOGDIR)
