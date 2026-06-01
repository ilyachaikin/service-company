import React, { useState, useEffect } from 'react';
import {
  Table, Space, Tag, Card, Typography, message, Select, Dropdown, Button,
  Modal, Form, DatePicker, Checkbox, Empty, Spin, Divider,
} from 'antd';
import {
  CheckCircleOutlined, ClockCircleOutlined, CloseCircleOutlined, PlusOutlined,
  FileTextOutlined, EditOutlined, DeleteOutlined, EyeOutlined,
  PaperClipOutlined, FileExcelOutlined, FilePdfOutlined, FileWordOutlined,
  DownloadOutlined,
} from '@ant-design/icons';
import api from '../../api/client';
import { useAuth } from '../../store/AuthContext';
import dayjs from 'dayjs';

const { Title, Text } = Typography;

const statusColors: any = {
  0: { color: 'default', label: 'Черновик',  icon: <ClockCircleOutlined /> },
  1: { color: 'blue',    label: 'Выставлен', icon: <ClockCircleOutlined /> },
  2: { color: 'success', label: 'Оплачен',   icon: <CheckCircleOutlined /> },
  3: { color: 'error',   label: 'Просрочен', icon: <ClockCircleOutlined /> },
  4: { color: 'default', label: 'Отменён',   icon: <CloseCircleOutlined /> },
};

const InvoicesPage: React.FC = () => {
  const { user } = useAuth();
  const canCreate = user?.roles?.some(r => ['Admin', 'Accountant', 'Manager'].includes(r));

  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [params, setParams] = useState<any>({ page: 1, pageSize: 15, clientId: undefined, status: undefined });
  const [clients, setClients] = useState<any[]>([]);

  const [editOpen, setEditOpen] = useState(false);
  const [editingInvoice, setEditingInvoice] = useState<any>(null);
  const [editForm] = Form.useForm();
  const [saving, setSaving] = useState(false);

  const [actsOpen, setActsOpen] = useState(false);
  const [viewingInvoice, setViewingInvoice] = useState<any>(null);
  const [loadingDetail, setLoadingDetail] = useState(false);

  const [createOpen, setCreateOpen] = useState(false);
  const [createForm] = Form.useForm();
  const [selectedClientId, setSelectedClientId] = useState<string | null>(null);
  const [unassignedActs, setUnassignedActs] = useState<any[]>([]);
  const [loadingActs, setLoadingActs] = useState(false);
  const [selectedActIds, setSelectedActIds] = useState<string[]>([]);
  const [creating, setCreating] = useState(false);

  const fetchInvoices = async () => {
    setLoading(true);
    try {
      const res = await api.get('/Invoices', { params });
      setData(res.data.items);
      setTotal(res.data.totalCount);
    } catch { message.error('Не удалось загрузить счета'); }
    finally { setLoading(false); }
  };

  const fetchClients = async () => {
    try {
      const res = await api.get('/Clients', { params: { pageSize: 100 } });
      setClients(res.data.items);
    } catch { }
  };

  const fetchUnassignedActs = async (clientId: string) => {
    setLoadingActs(true);
    try {
      const res = await api.get('/WorkActs/unassigned', { params: { clientId } });
      setUnassignedActs(res.data);
    } catch {
      message.error('Не удалось загрузить акты');
    } finally {
      setLoadingActs(false);
    }
  };

  useEffect(() => { fetchInvoices(); }, [params]);
  useEffect(() => { fetchClients(); }, []);

  const handleClientSelect = (clientId: string) => {
    setSelectedClientId(clientId);
    setSelectedActIds([]);
    setUnassignedActs([]);
    if (clientId) fetchUnassignedActs(clientId);
  };

  const handleCreateInvoice = async () => {
    try {
      const values = await createForm.validateFields();
      if (selectedActIds.length === 0) {
        message.warning('Выберите хотя бы один акт выполненных работ');
        return;
      }
      setCreating(true);
      await api.post('/Invoices', {
        clientId: selectedClientId,
        workActIds: selectedActIds,
        dueDate: values.dueDate.toISOString(),
      });
      message.success('Счёт выставлен успешно');
      setCreateOpen(false);
      createForm.resetFields();
      setSelectedClientId(null);
      setSelectedActIds([]);
      setUnassignedActs([]);
      fetchInvoices();
    } catch (err: any) {
      const data = err?.response?.data;
      const msg = data?.detail || data?.error || data?.title || 'Ошибка при создании счёта';
      message.error(msg);
    } finally {
      setCreating(false);
    }
  };

  const openEdit = (record: any) => {
    setEditingInvoice(record);
    editForm.setFieldsValue({ dueDate: dayjs(record.dueDate) });
    setEditOpen(true);
  };

  const handleEditSave = async () => {
    try {
      const values = await editForm.validateFields();
      setSaving(true);
      await api.put(`/Invoices/${editingInvoice.id}`, {
        id: editingInvoice.id,
        dueDate: values.dueDate.toISOString(),
        notes: null,
      });
      message.success('Счёт обновлён');
      setEditOpen(false);
      fetchInvoices();
    } catch (err: any) {
      const d = err?.response?.data;
      message.error(d?.detail || d?.error || d?.title || 'Ошибка при сохранении');
    } finally {
      setSaving(false);
    }
  };

  const handleStatusUpdate = async (id: string, status: number) => {
    try {
      await api.patch(`/Invoices/${id}/status`, { id, status });
      message.success('Статус обновлён');
      fetchInvoices();
    } catch { message.error('Не удалось обновить статус'); }
  };

  const handleDelete = (record: any) => {
    Modal.confirm({
      title: `Удалить счёт «${record.number}»?`,
      content: 'Счёт будет помечен как удалённый. Это действие нельзя отменить.',
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await api.delete(`/Invoices/${record.id}`);
          message.success('Счёт удалён');
          fetchInvoices();
        } catch {
          message.error('Не удалось удалить счёт');
        }
      },
    });
  };

  const downloadAttachment = async (attachmentId: string, fileName: string) => {
    try {
      const res = await api.get(`/WorkActs/attachments/${attachmentId}/download`, {
        responseType: 'blob',
      });
      const url = URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch {
      message.error('Не удалось скачать файл');
    }
  };

  const openActsModal = async (record: any) => {
    setActsOpen(true);
    setViewingInvoice({ number: record.number, workActs: [] });
    setLoadingDetail(true);
    try {
      const res = await api.get(`/Invoices/${record.id}`);
      setViewingInvoice(res.data);
    } catch {
      message.error('Не удалось загрузить детали счёта');
      setActsOpen(false);
    } finally {
      setLoadingDetail(false);
    }
  };

  const totalSelected = unassignedActs
    .filter(a => selectedActIds.includes(a.id))
    .reduce((s, a) => s + a.totalCost, 0);

  const columns = [
    { title: 'Номер', dataIndex: 'number', key: 'number', render: (t: string) => <b>{t}</b> },
    { title: 'Клиент', dataIndex: 'clientName', key: 'clientName' },
    {
      title: 'Сумма', dataIndex: 'amount', key: 'amount',
      render: (a: number) => <span style={{ fontWeight: 600 }}>{a.toLocaleString()} ₸</span>
    },
    {
      title: 'Дата выставления', dataIndex: 'issuedDate', key: 'issuedDate',
      render: (d: string) => dayjs(d).format('DD.MM.YYYY')
    },
    {
      title: 'Срок оплаты', dataIndex: 'dueDate', key: 'dueDate',
      render: (d: string) => dayjs(d).format('DD.MM.YYYY')
    },
    {
      title: 'Статус', dataIndex: 'status', key: 'status',
      render: (s: number, record: any) => (
        <Dropdown
          menu={{
            items: Object.entries(statusColors).map(([key, val]: any) => ({
              key,
              label: val.label,
              onClick: () => handleStatusUpdate(record.id, parseInt(key))
            }))
          }}
          trigger={['click']}
        >
          <Tag color={statusColors[s]?.color} icon={statusColors[s]?.icon} style={{ cursor: 'pointer' }}>
            {statusColors[s]?.label} ▾
          </Tag>
        </Dropdown>
      )
    },
    {
      title: 'Дата оплаты', dataIndex: 'paidDate', key: 'paidDate',
      render: (d: string) => d ? dayjs(d).format('DD.MM.YYYY') : '—'
    },
    {
      title: 'Акты', dataIndex: 'workActs', key: 'workActs',
      render: (acts: any[], record: any) => {
        if (!acts?.length) return <Tag>—</Tag>;
        return (
          <Tag
            color="blue"
            icon={<EyeOutlined />}
            style={{ cursor: 'pointer' }}
            onClick={() => openActsModal(record)}
          >
            {acts.length} акт(а)
          </Tag>
        );
      }
    },
    ...(canCreate ? [{
      title: 'Действия',
      key: 'actions',
      width: 100,
      render: (_: any, record: any) => (
        <Space size={4}>
          <Button
            size="small"
            type="text"
            icon={<EditOutlined />}
            onClick={() => openEdit(record)}
            title="Редактировать срок оплаты"
          />
          <Button
            size="small"
            type="text"
            danger
            icon={<DeleteOutlined />}
            onClick={() => handleDelete(record)}
            title="Удалить счёт"
          />
        </Space>
      ),
    }] : []),
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Счета</Title>
        {canCreate && (
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
            Выставить счёт
          </Button>
        )}
      </div>

      <Card>
        <div className="filter-bar" style={{ marginBottom: 16, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <Select
            placeholder="Фильтр по клиенту"
            style={{ width: 250, minWidth: 160 }}
            allowClear
            onChange={(val) => setParams({ ...params, clientId: val, page: 1 })}
          >
            {clients.map((c: any) => <Select.Option key={c.id} value={c.id}>{c.name}</Select.Option>)}
          </Select>
          <Select
            placeholder="Статус"
            style={{ width: 150, minWidth: 120 }}
            allowClear
            onChange={(val) => setParams({ ...params, status: val, page: 1 })}
          >
            {Object.entries(statusColors).map(([key, val]: any) => (
              <Select.Option key={key} value={parseInt(key)}>{val.label}</Select.Option>
            ))}
          </Select>
        </div>

        <Table
          columns={columns}
          dataSource={data}
          loading={loading}
          rowKey="id"
          scroll={{ x: 800 }}
          pagination={{
            current: params.page,
            pageSize: params.pageSize,
            total,
            onChange: (page, pageSize) => setParams({ ...params, page, pageSize })
          }}
        />
      </Card>

      {}
      <Modal
        title={<Space><FileTextOutlined /> Акты счёта {viewingInvoice?.number}</Space>}
        open={actsOpen}
        onCancel={() => { setActsOpen(false); setViewingInvoice(null); }}
        footer={<Button onClick={() => { setActsOpen(false); setViewingInvoice(null); }}>Закрыть</Button>}
        width="min(700px, 95vw)"
      >
        {loadingDetail ? (
          <div style={{ textAlign: 'center', padding: 48 }}><Spin size="large" tip="Загрузка актов..." /></div>
        ) : viewingInvoice?.workActs?.length ? (
          <>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              {viewingInvoice.workActs.map((act: any) => {
                const fileIcon = (name: string) => {
                  const ext = name.split('.').pop()?.toLowerCase();
                  if (ext === 'pdf') return <FilePdfOutlined style={{ color: '#ff4d4f' }} />;
                  if (ext === 'xlsx' || ext === 'xls') return <FileExcelOutlined style={{ color: '#52c41a' }} />;
                  if (ext === 'docx' || ext === 'doc') return <FileWordOutlined style={{ color: '#1890ff' }} />;
                  return <PaperClipOutlined />;
                };
                const fmtSize = (bytes: number) => {
                  if (bytes >= 1_048_576) return `${(bytes / 1_048_576).toFixed(1)} МБ`;
                  if (bytes >= 1_024) return `${(bytes / 1_024).toFixed(0)} КБ`;
                  return `${bytes} Б`;
                };
                return (
                  <Card
                    key={act.id}
                    size="small"
                    style={{ border: '1px solid #e8e8e8', borderRadius: 8 }}
                    bodyStyle={{ padding: '12px 16px' }}
                  >
                    {}
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 8 }}>
                      <div>
                        <Text strong style={{ fontSize: 15 }}>{act.number}</Text>
                        <div style={{ color: '#888', fontSize: 12, marginTop: 2 }}>
                          {dayjs(act.workDate).format('DD.MM.YYYY')}
                        </div>
                      </div>
                      <Text strong style={{ fontSize: 15, color: '#1890ff' }}>
                        {act.totalCost.toLocaleString()} ₸
                      </Text>
                    </div>

                    {}
                    {act.description && (
                      <div style={{ color: '#444', fontSize: 13, marginBottom: 8, borderLeft: '3px solid #f0f0f0', paddingLeft: 8 }}>
                        {act.description}
                      </div>
                    )}

                    {}
                    <Space size={16} style={{ marginBottom: act.attachments?.length ? 10 : 0 }}>
                      <Text type="secondary" style={{ fontSize: 12 }}>
                        Труд: <b>{act.laborCost.toLocaleString()} ₸</b>
                      </Text>
                      <Text type="secondary" style={{ fontSize: 12 }}>
                        Материалы: <b>{act.materialsCost.toLocaleString()} ₸</b>
                      </Text>
                    </Space>

                    {}
                    {act.attachments?.length > 0 ? (
                      <div style={{ marginTop: 8, borderTop: '1px solid #f5f5f5', paddingTop: 8 }}>
                        <Text type="secondary" style={{ fontSize: 12, marginBottom: 6, display: 'block' }}>
                          <PaperClipOutlined /> Вложения ({act.attachments.length}):
                        </Text>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                          {act.attachments.map((att: any) => (
                            <div
                              key={att.id}
                              style={{
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'space-between',
                                padding: '4px 8px',
                                background: '#fafafa',
                                borderRadius: 4,
                                border: '1px solid #f0f0f0',
                              }}
                            >
                              <Space size={6}>
                                {fileIcon(att.fileName)}
                                <Text style={{ fontSize: 13 }}>{att.fileName}</Text>
                                <Text type="secondary" style={{ fontSize: 11 }}>({fmtSize(att.fileSize)})</Text>
                              </Space>
                              <Button
                                type="link"
                                size="small"
                                icon={<DownloadOutlined />}
                                onClick={() => downloadAttachment(att.id, att.fileName)}
                              >
                                Скачать
                              </Button>
                            </div>
                          ))}
                        </div>
                      </div>
                    ) : (
                      <div style={{ marginTop: 8, borderTop: '1px solid #f5f5f5', paddingTop: 6 }}>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          <PaperClipOutlined /> Нет вложений
                        </Text>
                      </div>
                    )}
                  </Card>
                );
              })}
            </div>

            <div style={{ marginTop: 16, textAlign: 'right', padding: '12px 0', borderTop: '2px solid #f0f0f0' }}>
              <Text style={{ fontSize: 14 }}>Итого по счёту: </Text>
              <Text strong style={{ fontSize: 18, color: '#1890ff' }}>
                {viewingInvoice.workActs.reduce((s: number, a: any) => s + a.totalCost, 0).toLocaleString()} ₸
              </Text>
            </div>
          </>
        ) : (
          <Empty description="Нет прикреплённых актов" />
        )}
      </Modal>

      {}
      <Modal
        title={`Редактировать счёт ${editingInvoice?.number}`}
        open={editOpen}
        onCancel={() => { setEditOpen(false); editForm.resetFields(); }}
        onOk={handleEditSave}
        okText="Сохранить"
        cancelText="Отмена"
        confirmLoading={saving}
        width="min(420px, 95vw)"
      >
        <Form form={editForm} layout="vertical">
          <Form.Item
            name="dueDate"
            label="Срок оплаты"
            rules={[{ required: true, message: 'Укажите срок оплаты' }]}
          >
            <DatePicker style={{ width: '100%' }} format="DD.MM.YYYY" placeholder="Выберите дату" />
          </Form.Item>
        </Form>
      </Modal>

      {}
      <Modal
        title={<Space><FileTextOutlined /> Выставить счёт клиенту</Space>}
        open={createOpen}
        onCancel={() => {
          setCreateOpen(false);
          createForm.resetFields();
          setSelectedClientId(null);
          setSelectedActIds([]);
          setUnassignedActs([]);
        }}
        onOk={handleCreateInvoice}
        okText="Выставить счёт"
        cancelText="Отмена"
        confirmLoading={creating}
        width="min(680px, 95vw)"
      >
        <Form form={createForm} layout="vertical">
          <Form.Item label="Клиент" required>
            <Select
              placeholder="Выберите клиента"
              onChange={handleClientSelect}
              showSearch
              filterOption={(input, option) =>
                ((option?.children as unknown) as string)?.toLowerCase().includes(input.toLowerCase())
              }
            >
              {clients.map((c: any) => (
                <Select.Option key={c.id} value={c.id}>{c.name}</Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            name="dueDate"
            label="Срок оплаты"
            rules={[{ required: true, message: 'Укажите срок оплаты' }]}
          >
            <DatePicker
              style={{ width: '100%' }}
              disabledDate={(d) => d.isBefore(dayjs())}
              placeholder="Выберите дату"
            />
          </Form.Item>
        </Form>

        <Divider>Акты выполненных работ</Divider>

        {!selectedClientId ? (
          <div style={{ textAlign: 'center', color: '#888', padding: '16px 0' }}>
            Выберите клиента, чтобы увидеть доступные акты
          </div>
        ) : loadingActs ? (
          <div style={{ textAlign: 'center', padding: 24 }}><Spin /></div>
        ) : unassignedActs.length === 0 ? (
          <Empty description="Нет актов без счёта для этого клиента" />
        ) : (
          <>
            <div style={{ maxHeight: 280, overflowY: 'auto', border: '1px solid #f0f0f0', borderRadius: 6, padding: 8 }}>
              {unassignedActs.map((act: any) => (
                <div
                  key={act.id}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    padding: '8px 12px',
                    borderRadius: 4,
                    marginBottom: 4,
                    cursor: 'pointer',
                    background: selectedActIds.includes(act.id) ? '#e6f7ff' : 'transparent',
                  }}
                  onClick={() =>
                    setSelectedActIds(prev =>
                      prev.includes(act.id) ? prev.filter(x => x !== act.id) : [...prev, act.id]
                    )
                  }
                >
                  <Space>
                    <Checkbox checked={selectedActIds.includes(act.id)} />
                    <div>
                      <Text strong>{act.number}</Text>
                      <div style={{ fontSize: 12, color: '#888' }}>
                        {act.ticketTitle} • {dayjs(act.workDate).format('DD.MM.YYYY')}
                      </div>
                    </div>
                  </Space>
                  <Text strong>{act.totalCost.toLocaleString()} ₸</Text>
                </div>
              ))}
            </div>
            {selectedActIds.length > 0 && (
              <div style={{ marginTop: 12, textAlign: 'right' }}>
                <Text>Выбрано {selectedActIds.length} акт(а) на сумму </Text>
                <Text strong style={{ fontSize: 16 }}>{totalSelected.toLocaleString()} ₸</Text>
              </div>
            )}
          </>
        )}
      </Modal>
    </div>
  );
};

export default InvoicesPage;
