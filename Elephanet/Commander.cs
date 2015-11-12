using System.Collections.Generic;

namespace Elephanet
{
    public class Commander
    {
        IList<object> _updates;
        readonly int _batchSize;
        private ISchemaGenerator _schemaGenerator;

        public Commander(IList<object> updates, int batchSize = 1000)
        {
            _updates = updates;
            _batchSize = batchSize;
        }

        public int BatchSize
        {
            get
            {
                return _batchSize;
            }
        }

        public IList<BatchedEntities> Batch()
        {
            List<BatchedEntities> batches = new List<BatchedEntities>();
            List<object> batch = new List<object>();
            for (int i = 0; i < _updates.Count; i++)
            {
                batch.Add(_updates[i]);
                int batchCheck = i + 1;
                if (batchCheck % (_batchSize) == 0)
                {
                    batches.Add(new BatchedEntities(batch));
                    batch = new List<object>();
                }
            }

            //add the last batch on the end
            batches.Add(new BatchedEntities(batch));
            return batches;
        }
    }

    public class BatchedEntities
    {
        public BatchedEntities(List<object> entities)
        {
            Entities = entities;
        }
        public List<object> Entities { get; set; }
    }
}